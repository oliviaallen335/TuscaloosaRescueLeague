using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Services;

public class ApplicantSelfService
{
    private readonly AdoptionDbContext _db;

    public ApplicantSelfService(AdoptionDbContext db) => _db = db;

    public async Task<ApplicantProfileDto?> GetProfileAsync(int applicantId, CancellationToken ct = default)
    {
        var a = await _db.Applicants.FirstOrDefaultAsync(x => x.Id == applicantId, ct);
        return a == null ? null : ToDto(a);
    }

    public async Task<ApplicantProfileDto?> UpdateProfileAsync(int applicantId, ApplicantProfileUpdateDto dto, CancellationToken ct = default)
    {
        var a = await _db.Applicants.FirstOrDefaultAsync(x => x.Id == applicantId, ct);
        if (a == null)
            return null;

        if (dto.FirstName != null) a.FirstName = dto.FirstName;
        if (dto.LastName != null) a.LastName = dto.LastName;
        if (dto.Email != null) a.Email = dto.Email;
        if (dto.Phone != null) a.Phone = dto.Phone;
        if (dto.AddressLine1 != null) a.AddressLine1 = dto.AddressLine1;
        if (dto.AddressLine2 != null) a.AddressLine2 = dto.AddressLine2;
        if (dto.City != null) a.City = dto.City;
        if (dto.State != null) a.State = dto.State;
        if (dto.PostalCode != null) a.PostalCode = dto.PostalCode;
        if (dto.HasKids.HasValue) a.HasKids = dto.HasKids;
        if (dto.HasCats.HasValue) a.HasCats = dto.HasCats;
        if (dto.HasDogs.HasValue) a.HasDogs = dto.HasDogs;
        if (dto.HasYard.HasValue) a.HasYard = dto.HasYard;
        if (dto.HousingType != null) a.HousingType = dto.HousingType;
        if (dto.ExperienceLevel != null) a.ExperienceLevel = dto.ExperienceLevel;

        a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDto(a);
    }

    private static ApplicantProfileDto ToDto(Applicant a) =>
        new(
            a.Id,
            a.FirstName,
            a.LastName,
            a.Email,
            a.Phone,
            a.AddressLine1,
            a.AddressLine2,
            a.City,
            a.State,
            a.PostalCode,
            a.HasKids,
            a.HasCats,
            a.HasDogs,
            a.HasYard,
            a.HousingType,
            a.ExperienceLevel);
}
