using AdoptionAgency.Api.Extensions;
using AdoptionAgency.Api.Models;
using AdoptionAgency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Applicant")]
public class MeController : ControllerBase
{
    private readonly ApplicantSelfService _self;
    private readonly MatchingService _matching;

    public MeController(ApplicantSelfService self, MatchingService matching)
    {
        _self = self;
        _matching = matching;
    }

    [HttpGet("applicant")]
    public async Task<ActionResult<ApplicantProfileDto>> GetProfile()
    {
        var aid = User.GetApplicantId();
        if (aid == null) return Forbid();
        var p = await _self.GetProfileAsync(aid.Value);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPut("applicant")]
    public async Task<ActionResult<ApplicantProfileDto>> UpdateProfile([FromBody] ApplicantProfileUpdateDto dto)
    {
        var aid = User.GetApplicantId();
        if (aid == null) return Forbid();
        var p = await _self.UpdateProfileAsync(aid.Value, dto);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpGet("matches")]
    public async Task<ActionResult<IReadOnlyList<ScoredMatchDto>>> Matches()
    {
        var aid = User.GetApplicantId();
        if (aid == null) return Forbid();
        return Ok(await _matching.ListPreviewAsync(aid.Value));
    }
}
