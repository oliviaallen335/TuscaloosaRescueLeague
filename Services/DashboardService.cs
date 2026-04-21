using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Services;

public class DashboardService
{
    private readonly AdoptionDbContext _db;

    public DashboardService(AdoptionDbContext db) => _db = db;

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var animals = await _db.Animals.GroupBy(a => a.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        int C(AnimalStatus s) => animals.FirstOrDefault(x => x.Key == s)?.Count ?? 0;

        var openFosters = await _db.Placements.CountAsync(p =>
            p.PlacementType == PlacementType.Foster && p.EndDate == null, ct);

        return new DashboardStatsDto(
            C(AnimalStatus.Intake),
            C(AnimalStatus.InFoster),
            C(AnimalStatus.Adoptable),
            C(AnimalStatus.Adopted),
            C(AnimalStatus.Hold),
            C(AnimalStatus.MedicalHold),
            openFosters);
    }
}
