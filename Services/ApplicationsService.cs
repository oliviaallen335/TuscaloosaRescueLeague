using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Services;

public class ApplicationsService
{
    private readonly AdoptionDbContext _db;

    public ApplicationsService(AdoptionDbContext db) => _db = db;

    public async Task<(ApplicationDetailDto? Dto, string? Error)> CreateAsync(int applicantId, ApplicationCreateDto dto, CancellationToken ct = default)
    {
        var animal = await _db.Animals.FirstOrDefaultAsync(a => a.PublicId == dto.AnimalPublicId, ct);
        if (animal == null)
            return (null, "Animal not found");

        if (animal.Status != AnimalStatus.Adoptable && animal.Status != AnimalStatus.InFoster && animal.Status != AnimalStatus.Intake)
            return (null, "This animal is not accepting applications right now");

        var dup = await _db.Applications.AnyAsync(x =>
            x.ApplicantId == applicantId && x.AnimalId == animal.Id &&
            (x.Status == ApplicationStatus.Pending), ct);
        if (dup)
            return (null, "You already have a pending application for this animal");

        var app = new Application
        {
            ApplicantId = applicantId,
            AnimalId = animal.Id,
            Status = ApplicationStatus.Pending,
            AnswersJson = dto.AnswersJson
        };
        _db.Applications.Add(app);
        await _db.SaveChangesAsync(ct);

        return (await GetDetailAsync(app.Id, applicantId, isEmployee: false, ct), null);
    }

    public async Task<IReadOnlyList<ApplicationListDto>> ListForApplicantAsync(int applicantId, CancellationToken ct = default)
    {
        return await BaseQuery()
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(a => new ApplicationListDto(
                a.Id,
                a.ApplicantId,
                a.Applicant.FirstName + " " + a.Applicant.LastName,
                a.Animal.PublicId,
                a.Animal.Name,
                a.Status,
                a.SubmittedAt,
                a.ReviewedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ApplicationListDto>> ListForEmployeeAsync(
        ApplicationStatus? status, int? applicantId, string? animalPublicId, CancellationToken ct = default)
    {
        var q = BaseQuery();
        if (status.HasValue)
            q = q.Where(a => a.Status == status.Value);
        if (applicantId.HasValue)
            q = q.Where(a => a.ApplicantId == applicantId.Value);
        if (!string.IsNullOrWhiteSpace(animalPublicId))
        {
            var pid = animalPublicId.Trim();
            q = q.Where(a => a.Animal.PublicId == pid);
        }

        return await q.OrderByDescending(a => a.SubmittedAt)
            .Select(a => new ApplicationListDto(
                a.Id,
                a.ApplicantId,
                a.Applicant.FirstName + " " + a.Applicant.LastName,
                a.Animal.PublicId,
                a.Animal.Name,
                a.Status,
                a.SubmittedAt,
                a.ReviewedAt))
            .ToListAsync(ct);
    }

    public async Task<ApplicationDetailDto?> GetDetailAsync(int id, int? applicantId, bool isEmployee, CancellationToken ct = default)
    {
        var q = BaseQuery().Where(a => a.Id == id);
        if (!isEmployee)
        {
            if (!applicantId.HasValue)
                return null;
            q = q.Where(a => a.ApplicantId == applicantId.Value);
        }

        var row = await q.FirstOrDefaultAsync(ct);
        return row == null ? null : ToDetail(row);
    }

    public async Task<(ApplicationDetailDto? Dto, string? Error)> ReviewAsync(int id, ApplicationReviewDto dto, CancellationToken ct = default)
    {
        var app = await _db.Applications.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (app == null)
            return (null, "Application not found");

        app.Status = dto.Status;
        app.Notes = dto.Notes;
        app.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return (await GetDetailAsync(id, null, isEmployee: true, ct), null);
    }

    /// <summary>Applicant withdraws their own pending application.</summary>
    public async Task<(ApplicationDetailDto? Dto, string? Error)> WithdrawAsync(int id, int applicantId, CancellationToken ct = default)
    {
        var app = await _db.Applications.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (app == null)
            return (null, "Application not found");
        if (app.ApplicantId != applicantId)
            return (null, "Not your application");
        if (app.Status != ApplicationStatus.Pending)
            return (null, "Only pending applications can be withdrawn");

        app.Status = ApplicationStatus.Withdrawn;
        app.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return (await GetDetailAsync(id, applicantId, isEmployee: false, ct), null);
    }

    private IQueryable<Application> BaseQuery() =>
        _db.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Animal);

    private static ApplicationDetailDto ToDetail(Application a) =>
        new(
            a.Id,
            a.ApplicantId,
            $"{a.Applicant.FirstName} {a.Applicant.LastName}",
            a.Applicant.Email ?? "",
            a.Animal.PublicId,
            a.Animal.Name,
            a.Status,
            a.SubmittedAt,
            a.ReviewedAt,
            a.Notes,
            a.AnswersJson);
}
