using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Employee")]
public class ApplicantsController : ControllerBase
{
    private readonly AdoptionDbContext _db;

    public ApplicantsController(AdoptionDbContext db) => _db = db;

    /// <summary>Search applicants for placement / admin (name or email).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApplicantListDto>>> Search([FromQuery] string? q)
    {
        var query = _db.Applicants.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(a =>
                a.FirstName.Contains(s) ||
                a.LastName.Contains(s) ||
                (a.Email != null && a.Email.Contains(s)));
        }

        var list = await query
            .OrderBy(a => a.LastName)
            .Take(50)
            .Select(a => new ApplicantListDto(a.Id, a.FirstName, a.LastName, a.Email))
            .ToListAsync();
        return Ok(list);
    }
}
