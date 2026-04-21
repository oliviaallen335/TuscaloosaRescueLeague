using AdoptionAgency.Api.Models;
using AdoptionAgency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboard;

    public DashboardController(DashboardService dashboard) => _dashboard = dashboard;

    [Authorize(Roles = "Employee")]
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> Stats()
        => Ok(await _dashboard.GetStatsAsync());
}
