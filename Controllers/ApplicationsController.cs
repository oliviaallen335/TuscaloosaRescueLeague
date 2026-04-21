using AdoptionAgency.Api.Extensions;
using AdoptionAgency.Api.Models;
using AdoptionAgency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly ApplicationsService _apps;

    public ApplicationsController(ApplicationsService apps) => _apps = apps;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApplicationListDto>>> List(
        [FromQuery] ApplicationStatus? status,
        [FromQuery] int? applicantId,
        [FromQuery] string? animalPublicId)
    {
        if (User.IsEmployee())
            return Ok(await _apps.ListForEmployeeAsync(status, applicantId, animalPublicId));

        var aid = User.GetApplicantId();
        if (aid == null)
            return Forbid();
        return Ok(await _apps.ListForApplicantAsync(aid.Value));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApplicationDetailDto>> Get(int id)
    {
        var isEmp = User.IsEmployee();
        var aid = User.GetApplicantId();
        var dto = await _apps.GetDetailAsync(id, aid, isEmp);
        return dto == null ? NotFound() : Ok(dto);
    }

    [Authorize(Roles = "Applicant")]
    [HttpPost]
    public async Task<ActionResult<ApplicationDetailDto>> Create([FromBody] ApplicationCreateDto dto)
    {
        var aid = User.GetApplicantId();
        if (aid == null)
            return Forbid();

        var (result, error) = await _apps.CreateAsync(aid.Value, dto);
        if (error != null)
            return BadRequest(new { error });
        return CreatedAtAction(nameof(Get), new { id = result!.Id }, result);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<ApplicationDetailDto>> Review(int id, [FromBody] ApplicationReviewDto dto)
    {
        if (User.IsEmployee())
        {
            var (result, error) = await _apps.ReviewAsync(id, dto);
            if (error != null)
                return BadRequest(new { error });
            return Ok(result);
        }
        var aid = User.GetApplicantId();
        if (aid == null)
            return Forbid();
        if (dto.Status != ApplicationStatus.Withdrawn)
            return BadRequest(new { error = "Applicants can only withdraw their own applications." });
        var (withdrawResult, withdrawError) = await _apps.WithdrawAsync(id, aid.Value);
        if (withdrawError != null)
            return BadRequest(new { error = withdrawError });
        return Ok(withdrawResult!);
    }
}
