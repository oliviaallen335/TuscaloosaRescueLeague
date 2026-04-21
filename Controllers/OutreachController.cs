using AdoptionAgency.Api.Models;
using AdoptionAgency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OutreachController : ControllerBase
{
    private readonly OutreachService _outreach;

    public OutreachController(OutreachService outreach) => _outreach = outreach;

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OutreachEventDto>>> ListUpcoming()
        => Ok(await _outreach.ListUpcomingAsync(employeeView: false));

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OutreachEventDto>> Get(int id)
    {
        var e = await _outreach.GetAsync(id, employeeView: false);
        return e == null ? NotFound() : Ok(e);
    }

    [Authorize(Roles = "Employee")]
    [HttpGet("all")]
    public async Task<ActionResult<IReadOnlyList<OutreachEventDto>>> ListAll()
        => Ok(await _outreach.ListUpcomingAsync(employeeView: true));

    [Authorize(Roles = "Employee")]
    [HttpPost]
    public async Task<ActionResult<OutreachEventDto>> Create([FromBody] OutreachEventCreateDto dto)
    {
        var e = await _outreach.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = e!.Id }, e);
    }

    [Authorize(Roles = "Employee")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<OutreachEventDto>> Update(int id, [FromBody] OutreachEventCreateDto dto)
    {
        var e = await _outreach.UpdateAsync(id, dto);
        return e == null ? NotFound() : Ok(e);
    }

    [Authorize(Roles = "Employee")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _outreach.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
