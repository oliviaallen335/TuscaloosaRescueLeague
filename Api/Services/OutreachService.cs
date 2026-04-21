using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Services;

public class OutreachService
{
    private readonly AdoptionDbContext _db;

    public OutreachService(AdoptionDbContext db) => _db = db;

    public async Task<IReadOnlyList<OutreachEventDto>> ListUpcomingAsync(bool employeeView, CancellationToken ct = default)
    {
        var q = _db.OutreachEvents.AsQueryable();
        if (!employeeView)
        {
            var today = DateTime.UtcNow.Date;
            q = q.Where(e => e.IsPublished && (e.EndDate == null || e.EndDate >= today));
        }
        var list = await q.OrderBy(e => e.StartDate).ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }

    public async Task<OutreachEventDto?> GetAsync(int id, bool employeeView, CancellationToken ct = default)
    {
        var e = await _db.OutreachEvents.FindAsync(new object[] { id }, ct);
        if (e == null) return null;
        if (!employeeView && (!e.IsPublished || (e.EndDate.HasValue && e.EndDate.Value.Date < DateTime.UtcNow.Date)))
            return null;
        return ToDto(e);
    }

    public async Task<OutreachEventDto?> CreateAsync(OutreachEventCreateDto dto, CancellationToken ct = default)
    {
        var e = new OutreachEvent
        {
            Title = dto.Title,
            Description = dto.Description,
            Type = dto.Type,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Location = dto.Location,
            Url = dto.Url,
            IsPublished = dto.IsPublished
        };
        _db.OutreachEvents.Add(e);
        await _db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    public async Task<OutreachEventDto?> UpdateAsync(int id, OutreachEventCreateDto dto, CancellationToken ct = default)
    {
        var e = await _db.OutreachEvents.FindAsync(new object[] { id }, ct);
        if (e == null) return null;
        e.Title = dto.Title;
        e.Description = dto.Description;
        e.Type = dto.Type;
        e.StartDate = dto.StartDate;
        e.EndDate = dto.EndDate;
        e.Location = dto.Location;
        e.Url = dto.Url;
        e.IsPublished = dto.IsPublished;
        await _db.SaveChangesAsync(ct);
        return ToDto(e);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var e = await _db.OutreachEvents.FindAsync(new object[] { id }, ct);
        if (e == null) return false;
        _db.OutreachEvents.Remove(e);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static OutreachEventDto ToDto(OutreachEvent e) =>
        new(e.Id, e.Title, e.Description, e.Type, e.StartDate, e.EndDate, e.Location, e.Url, e.IsPublished, e.CreatedAt);
}
