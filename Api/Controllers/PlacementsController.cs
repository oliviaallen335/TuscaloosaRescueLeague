using System.Security.Claims;
using AdoptionAgency.Api.Models;
using AdoptionAgency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlacementsController : ControllerBase
{
    private readonly PlacementsService _placements;
    private readonly PaymentService _payments;

    public PlacementsController(PlacementsService placements, PaymentService payments)
    {
        _placements = placements;
        _payments = payments;
    }

    [Authorize(Roles = "Applicant")]
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<PlacementWithPaymentDto>>> ListMy(CancellationToken ct = default)
    {
        var applicantId = int.Parse(User.FindFirst(c => c.Type == "ApplicantId")?.Value ?? "0");
        if (applicantId == 0) return Unauthorized();
        return Ok(await _placements.ListForApplicantAsync(applicantId, ct));
    }

    [Authorize(Roles = "Employee")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlacementListDto>>> List(
        [FromQuery] string? animalPublicId,
        [FromQuery] int? applicantId)
        => Ok(await _placements.ListAsync(animalPublicId, applicantId));

    [Authorize(Roles = "Employee")]
    [HttpPost]
    public async Task<ActionResult<PlacementListDto>> Create([FromBody] PlacementCreateDto dto)
    {
        var (result, error) = await _placements.CreateAsync(dto);
        if (error != null)
            return BadRequest(new { error });
        return CreatedAtAction(nameof(List), new { animalPublicId = dto.AnimalPublicId }, result);
    }

    [Authorize(Roles = "Employee")]
    [HttpPost("{id:int}/end")]
    public async Task<ActionResult<PlacementListDto>> End(int id, [FromBody] PlacementEndDto dto)
    {
        var (result, error) = await _placements.EndPlacementAsync(id, dto.EndDate);
        if (error != null)
            return BadRequest(new { error });
        return Ok(result);
    }
}
