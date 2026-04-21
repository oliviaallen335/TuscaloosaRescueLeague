using AdoptionAgency.Api.Extensions;
using AdoptionAgency.Api.Models;
using AdoptionAgency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchController : ControllerBase
{
    private readonly MatchingService _matching;

    public MatchController(MatchingService matching) => _matching = matching;

    [AllowAnonymous]
    [HttpGet("questionnaire")]
    public ActionResult<QuestionnaireSchemaDto> Questionnaire()
        => Ok(MatchingService.QuestionnaireSchema);

    [Authorize(Roles = "Applicant")]
    [HttpPost("run")]
    public async Task<ActionResult<MatchRunResultDto>> Run([FromBody] MatchRunDto dto)
    {
        var aid = User.GetApplicantId();
        if (aid == null) return Forbid();

        var result = await _matching.RunAsync(aid.Value, dto);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
