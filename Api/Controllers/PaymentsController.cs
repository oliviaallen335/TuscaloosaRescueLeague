using System.Security.Claims;
using AdoptionAgency.Api.Models;
using AdoptionAgency.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptionAgency.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _payments;
    private readonly IConfiguration _config;

    public PaymentsController(PaymentService payments, IConfiguration config)
    {
        _payments = payments;
        _config = config;
    }

    /// <summary>Stripe webhook — no auth, validates signature.</summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature))
            return BadRequest();

        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(ct);
        var ok = await _payments.HandleWebhookAsync(json, signature, ct);
        return ok ? Ok() : BadRequest();
    }

    [Authorize(Roles = "Applicant")]
    [HttpGet("confirm-session")]
    public async Task<IActionResult> ConfirmSession([FromQuery] string session_id, CancellationToken ct = default)
    {
        var applicantId = GetApplicantId();
        if (applicantId == null)
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(session_id))
            return BadRequest(new { error = "Missing session_id" });

        var (synced, error) = await _payments.ConfirmSessionAsync(session_id, applicantId.Value, ct);
        if (error != null)
            return BadRequest(new { error });
        return Ok(new { synced });
    }

    [Authorize(Roles = "Applicant")]
    [HttpPost("create-checkout")]
    public async Task<ActionResult<PaymentCreateCheckoutResponse>> CreateCheckout([FromBody] PaymentCreateCheckoutRequest dto, CancellationToken ct = default)
    {
        var applicantId = GetApplicantId();
        if (applicantId == null)
            return Unauthorized();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var successUrl = dto.SuccessUrl.StartsWith("http") ? dto.SuccessUrl : baseUrl.TrimEnd('/') + (dto.SuccessUrl.StartsWith("/") ? "" : "/") + dto.SuccessUrl;
        var cancelUrl = dto.CancelUrl.StartsWith("http") ? dto.CancelUrl : baseUrl.TrimEnd('/') + (dto.CancelUrl.StartsWith("/") ? "" : "/") + dto.CancelUrl;

        var (url, error) = await _payments.CreateCheckoutSessionAsync(dto.PlacementId, applicantId.Value, successUrl, cancelUrl, ct);
        if (error != null)
            return BadRequest(new PaymentCreateCheckoutResponse(null, error));
        return Ok(new PaymentCreateCheckoutResponse(url, null));
    }

    [Authorize(Roles = "Applicant")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentStatusDto>>> ListMy(CancellationToken ct = default)
    {
        var applicantId = GetApplicantId();
        if (applicantId == null)
            return Unauthorized();

        var canView = User.FindFirst(c => c.Type == "CanViewPaymentDetails")?.Value == "True";
        return Ok(await _payments.ListByApplicantAsync(applicantId.Value, canView, ct));
    }

    [Authorize(Roles = "Applicant")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentStatusDto>> GetStatus(int id, CancellationToken ct = default)
    {
        var applicantId = GetApplicantId();
        if (applicantId == null)
            return Unauthorized();

        var canView = User.FindFirst(c => c.Type == "CanViewPaymentDetails")?.Value == "True";
        var dto = await _payments.GetStatusAsync(id, applicantId, canView, ct);
        return dto == null ? NotFound() : Ok(dto);
    }

    [Authorize(Roles = "Employee")]
    [HttpGet("by-placement/{placementId:int}")]
    public async Task<ActionResult<PaymentStatusDto>> GetByPlacement(int placementId, CancellationToken ct = default)
    {
        var canView = User.FindFirst(c => c.Type == "CanViewPaymentDetails")?.Value == "True";
        var payment = await _payments.GetByPlacementAsync(placementId, canView, ct);
        return payment == null ? NotFound() : Ok(payment);
    }

    private int? GetApplicantId()
    {
        var claim = User.FindFirst(c => c.Type == "ApplicantId")?.Value;
        return claim != null && int.TryParse(claim, out var id) ? id : null;
    }
}
