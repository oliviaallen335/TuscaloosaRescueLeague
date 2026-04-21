using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Services;

public class PlacementsService
{
    // Client pricing rules:
    // - Adult: $130
    // - Puppy/kitten: $70
    // - Dog adult threshold: 48 months
    // - Cat adult threshold: 12 months
    private const decimal AdultAdoptionFee = 130m;
    private const decimal YoungAdoptionFee = 70m;
    private const int DogAdultMonths = 48;
    private const int CatAdultMonths = 12;

    private readonly AdoptionDbContext _db;
    private readonly PaymentService _payments;

    public PlacementsService(AdoptionDbContext db, PaymentService payments)
    {
        _db = db;
        _payments = payments;
    }

    public async Task<IReadOnlyList<PlacementListDto>> ListAsync(string? animalPublicId, int? applicantId, CancellationToken ct = default)
    {
        var q = _db.Placements
            .Include(p => p.Animal)
            .Include(p => p.Applicant)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(animalPublicId))
        {
            var pid = animalPublicId.Trim();
            q = q.Where(p => p.Animal.PublicId == pid);
        }

        if (applicantId.HasValue)
            q = q.Where(p => p.ApplicantId == applicantId.Value);

        var list = await q.OrderByDescending(p => p.StartDate).ToListAsync(ct);
        var adoptionWithFee = list.Where(p => p.PlacementType == PlacementType.Adoption && p.Fee > 0).Select(p => p.Id).ToList();
        var paidIds = await _payments.GetPaidPlacementIdsAsync(adoptionWithFee, ct);

        return list.Select(p =>
        {
            var isPaid = p.PlacementType == PlacementType.Adoption && p.Fee > 0
                ? paidIds.Contains(p.Id)
                : (bool?)null;
            return new PlacementListDto(
                p.Id,
                p.Animal.PublicId,
                p.Animal.Name,
                p.ApplicantId,
                $"{p.Applicant.FirstName} {p.Applicant.LastName}",
                p.PlacementType,
                p.StartDate,
                p.EndDate,
                p.Fee,
                p.Notes,
                isPaid);
        }).ToList();
    }

    public async Task<(PlacementListDto? Dto, string? Error)> CreateAsync(PlacementCreateDto dto, CancellationToken ct = default)
    {
        var animal = await _db.Animals.FirstOrDefaultAsync(a => a.PublicId == dto.AnimalPublicId, ct);
        if (animal == null)
            return (null, "Animal not found");

        var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.Id == dto.ApplicantId, ct);
        if (applicant == null)
            return (null, "Applicant not found");

        if (dto.PlacementType == PlacementType.Adoption)
        {
            if (animal.Status == AnimalStatus.Adopted)
                return (null, "Animal is already adopted");
            var hasApprovedApp = await _db.Applications
                .AnyAsync(a => a.ApplicantId == dto.ApplicantId && a.AnimalId == animal.Id && a.Status == ApplicationStatus.Approved, ct);
            if (!hasApprovedApp)
                return (null, "Applicant must have an approved application for this animal before creating an adoption placement.");
        }

        var openFoster = await _db.Placements.AnyAsync(p =>
            p.AnimalId == animal.Id && p.PlacementType == PlacementType.Foster && p.EndDate == null, ct);
        if (dto.PlacementType == PlacementType.Foster && openFoster)
            return (null, "Animal already has an open foster placement; end it first");

        var openAdoption = await _db.Placements.AnyAsync(p =>
            p.AnimalId == animal.Id && p.PlacementType == PlacementType.Adoption && p.EndDate == null, ct);
        if (dto.PlacementType == PlacementType.Adoption && openAdoption)
            return (null, "Animal already has an open adoption placement");

        var placement = new Placement
        {
            AnimalId = animal.Id,
            ApplicantId = dto.ApplicantId,
            PlacementType = dto.PlacementType,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Fee = dto.PlacementType == PlacementType.Adoption ? ComputeAdoptionFee(animal) : dto.Fee,
            Notes = dto.Notes
        };
        _db.Placements.Add(placement);

        if (placement.EndDate == null)
            ApplyStatusForNewOpenPlacement(animal, dto.PlacementType);

        animal.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return (await GetSingleAsync(placement.Id, ct), null);
    }

    public async Task<(PlacementListDto? Dto, string? Error)> EndPlacementAsync(int id, DateTime endDate, CancellationToken ct = default)
    {
        var p = await _db.Placements
            .Include(x => x.Animal)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p == null)
            return (null, "Placement not found");
        if (p.EndDate != null)
            return (null, "Placement already ended");

        p.EndDate = endDate;
        p.Animal.UpdatedAt = DateTime.UtcNow;

        if (p.PlacementType == PlacementType.Foster)
        {
            var stillOpenFoster = await _db.Placements.AnyAsync(x =>
                x.AnimalId == p.AnimalId && x.PlacementType == PlacementType.Foster && x.EndDate == null && x.Id != p.Id, ct);
            if (!stillOpenFoster && p.Animal.Status == AnimalStatus.InFoster)
                p.Animal.Status = AnimalStatus.Adoptable;
        }

        await _db.SaveChangesAsync(ct);
        return (await GetSingleAsync(id, ct), null);
    }

    private static void ApplyStatusForNewOpenPlacement(Animal animal, PlacementType type)
    {
        if (type == PlacementType.Foster)
            animal.Status = AnimalStatus.InFoster;
        else if (type == PlacementType.Adoption)
            animal.Status = AnimalStatus.Adopted;
    }

    private static decimal ComputeAdoptionFee(Animal animal)
    {
        // If age is missing, default to adult pricing for safety/compliance.
        if (!animal.EstimatedAgeMonths.HasValue)
            return AdultAdoptionFee;

        var ageMonths = animal.EstimatedAgeMonths.Value;
        var adultThreshold = animal.Species == Species.Dog ? DogAdultMonths : CatAdultMonths;
        return ageMonths >= adultThreshold ? AdultAdoptionFee : YoungAdoptionFee;
    }

    public async Task<IReadOnlyList<PlacementWithPaymentDto>> ListForApplicantAsync(int applicantId, CancellationToken ct = default)
    {
        var list = await _db.Placements
            .Include(p => p.Animal)
            .Where(p => p.ApplicantId == applicantId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(ct);

        var result = new List<PlacementWithPaymentDto>();
        foreach (var p in list)
        {
            var needsPayment = p.PlacementType == PlacementType.Adoption && p.Fee.HasValue && p.Fee > 0;
            var isPaid = !needsPayment || await _payments.IsPlacementPaidAsync(p.Id, ct);
            var hasPending = needsPayment && !isPaid && await _payments.HasPendingPaymentAsync(p.Id, ct);
            result.Add(new PlacementWithPaymentDto(
                p.Id, p.Animal.PublicId, p.Animal.Name, p.PlacementType, p.StartDate, p.EndDate, p.Fee, isPaid, hasPending));
        }
        return result;
    }

    private async Task<PlacementListDto?> GetSingleAsync(int id, CancellationToken ct)
    {
        var p = await _db.Placements
            .Include(x => x.Animal)
            .Include(x => x.Applicant)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return p == null
            ? null
            : new PlacementListDto(
                p.Id,
                p.Animal.PublicId,
                p.Animal.Name,
                p.ApplicantId,
                $"{p.Applicant.FirstName} {p.Applicant.LastName}",
                p.PlacementType,
                p.StartDate,
                p.EndDate,
                p.Fee,
                p.Notes,
                null);
    }
}
