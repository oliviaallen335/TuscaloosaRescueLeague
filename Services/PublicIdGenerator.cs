using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Services;

public class PublicIdGenerator
{
    private readonly AdoptionDbContext _db;

    public PublicIdGenerator(AdoptionDbContext db) => _db = db;

    /// <summary>D-YYMMDD-NNN or C-YYMMDD-NNN (species + date + daily sequence).</summary>
    public async Task<string> NextAsync(Species species, DateTime? utcDate = null)
    {
        var d = (utcDate ?? DateTime.UtcNow).Date;
        var prefix = species == Species.Dog ? "D" : "C";
        var datePart = d.ToString("yyMMdd");
        var start = $"{prefix}-{datePart}-";

        var existing = await _db.Animals
            .Where(a => a.PublicId.StartsWith(start))
            .Select(a => a.PublicId)
            .ToListAsync();

        var max = 0;
        foreach (var id in existing)
        {
            var parts = id.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var n))
                max = Math.Max(max, n);
        }

        return $"{start}{(max + 1):D3}";
    }
}
