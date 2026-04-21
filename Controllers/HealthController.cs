using AdoptionAgency.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", app = "Tuscaloosa Rescue League API" });

    [HttpGet("db")]
    public async Task<IActionResult> CheckDb([FromServices] AdoptionDbContext db)
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync();
            return Ok(new { database = canConnect ? "connected" : "failed", provider = db.Database.ProviderName });
        }
        catch (Exception ex)
        {
            return Ok(new { database = "failed", error = ex.Message });
        }
    }
}
