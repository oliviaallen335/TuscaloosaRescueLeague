using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Services;

public class IntakesService
{
    private readonly AdoptionDbContext _db;

    public IntakesService(AdoptionDbContext db) => _db = db;

    public async Task<IReadOnlyList<IntakeListDto>> ListByAnimalPublicIdAsync(string publicId, CancellationToken ct = default)
    {
        var animal = await _db.Animals.FirstOrDefaultAsync(a => a.PublicId == publicId, ct);
        if (animal == null)
            return Array.Empty<IntakeListDto>();

        return await _db.Intakes
            .Where(i => i.AnimalId == animal.Id)
            .OrderByDescending(i => i.IntakeDate)
            .Select(i => new IntakeListDto(i.Id, i.Source, i.Notes, i.IntakeDate, i.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<(IntakeListDto? Dto, string? Error)> AddAsync(string publicId, IntakeCreateDto dto, CancellationToken ct = default)
    {
        var animal = await _db.Animals.FirstOrDefaultAsync(a => a.PublicId == publicId, ct);
        if (animal == null)
            return (null, "Animal not found");

        var intakeDate = dto.IntakeDate ?? DateTime.UtcNow;
        var row = new Intake
        {
            AnimalId = animal.Id,
            Source = dto.Source,
            Notes = dto.Notes,
            IntakeDate = intakeDate
        };
        _db.Intakes.Add(row);
        animal.IntakeDate = intakeDate;
        animal.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return (new IntakeListDto(row.Id, row.Source, row.Notes, row.IntakeDate, row.CreatedAt), null);
    }
}
