using AdoptionAgency.Api.Models;
using AdoptionAgency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var result = await _auth.LoginAsync(req.Email, req.Password);
        return result == null ? Unauthorized(new { error = "Invalid email or password" }) : Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var (response, error) = await _auth.RegisterApplicantAsync(req);
        if (error != null)
            return BadRequest(new { error });
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var me = await _auth.GetMeAsync(userId);
        return me == null ? NotFound() : Ok(me);
    }
}
