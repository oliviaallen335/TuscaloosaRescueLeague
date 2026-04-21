using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace AdoptionAgency.Api.Services;

public class PaymentService
{
    private readonly AdoptionDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _log;

    public PaymentService(AdoptionDbContext db, IConfiguration config, ILogger<PaymentService> log)
    {
        _db = db;
        _config = config;
        _log = log;
    }

    public async Task<(string? CheckoutUrl, string? Error)> CreateCheckoutSessionAsync(
        int placementId, int applicantId, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var key = _config["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(key))
            return (null, "Stripe not configured. Set Stripe:SecretKey.");

        var placement = await _db.Placements
            .Include(p => p.Animal)
            .Include(p => p.Applicant)
            .FirstOrDefaultAsync(p => p.Id == placementId, ct);
        if (placement == null)
            return (null, "Placement not found");
        if (placement.ApplicantId != applicantId)
            return (null, "Not your placement");
        if (placement.PlacementType != PlacementType.Adoption)
            return (null, "Only adoption placements require fee payment");
        if (placement.EndDate != null)
            return (null, "Placement already ended");

        var approvedApp = await _db.Applications
            .AnyAsync(a => a.ApplicantId == applicantId && a.AnimalId == placement.AnimalId && a.Status == ApplicationStatus.Approved, ct);
        if (!approvedApp)
            return (null, "Your adoption must be approved before you can pay the fee.");

        var amountCents = (int)Math.Round((placement.Fee ?? 0) * 100);
        if (amountCents < 50) // Stripe minimum
            return (null, "Adoption fee must be at least $0.50");

        var alreadyPaid = await _db.Payments
            .AnyAsync(p => p.PlacementId == placementId && p.Status == PaymentStatus.Completed, ct);
        if (alreadyPaid)
            return (null, "This adoption fee has already been paid");

        var pendingPayment = await _db.Payments
            .FirstOrDefaultAsync(p => p.PlacementId == placementId && p.Status == PaymentStatus.Pending, ct);
        if (pendingPayment != null)
            return (null, "A payment is already in progress. If you just completed payment, refresh the page. Otherwise wait a few minutes.");

        successUrl = successUrl + (successUrl.Contains('?') ? "&" : "?") + "session_id={CHECKOUT_SESSION_ID}";

        StripeConfiguration.ApiKey = key;
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = amountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Adoption fee — {placement.Animal.Name}",
                            Description = $"Adoption fee for {placement.Animal.Name} ({placement.Animal.PublicId})"
                        }
                    },
                    Quantity = 1
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["placementId"] = placementId.ToString(),
                ["animalPublicId"] = placement.Animal.PublicId
            }
        };

        try
        {
            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: ct);

            var payment = new Payment
            {
                AnimalId = placement.AnimalId,
                ApplicantId = applicantId,
                PlacementId = placementId,
                Amount = placement.Fee ?? 0,
                Status = PaymentStatus.Pending,
                StripeSessionId = session.Id
            };
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(ct);

            return (session.Url, null);
        }
        catch (StripeException ex)
        {
            _log.LogWarning(ex, "Stripe create session failed");
            return (null, ex.Message);
        }
    }

    /// <summary>Sync payment status from Stripe when user returns from checkout. Use when webhook may not have fired (e.g. local dev).</summary>
    public async Task<(bool Synced, string? Error)> ConfirmSessionAsync(string sessionId, int applicantId, CancellationToken ct = default)
    {
        var key = _config["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(key))
            return (false, "Stripe not configured");

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.StripeSessionId == sessionId, ct);
        if (payment == null)
            return (false, "Payment not found");
        if (payment.ApplicantId != applicantId)
            return (false, "Not your payment");
        if (payment.Status == PaymentStatus.Completed)
            return (true, null);

        StripeConfiguration.ApiKey = key;
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId, cancellationToken: ct);
            if (session.PaymentStatus == "paid")
            {
                payment.Status = PaymentStatus.Completed;
                payment.CompletedAt = DateTime.UtcNow;
                payment.StripePaymentIntentId = session.PaymentIntentId ?? session.PaymentIntent?.Id;
                await _db.SaveChangesAsync(ct);
                _log.LogInformation("Payment {Id} confirmed via session for placement {PlacementId}", payment.Id, payment.PlacementId);
                return (true, null);
            }
        }
        catch (StripeException ex)
        {
            _log.LogWarning(ex, "Stripe session fetch failed");
            return (false, ex.Message);
        }
        return (false, "Payment not yet completed");
    }

    public async Task<bool> HandleWebhookAsync(string json, string signature, CancellationToken ct = default)
    {
        var secret = _config["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            _log.LogWarning("Stripe:WebhookSecret not set");
            return false;
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, secret);
        }
        catch (StripeException ex)
        {
            _log.LogWarning(ex, "Stripe webhook signature invalid");
            return false;
        }

        if (stripeEvent.Type != Events.CheckoutSessionCompleted)
            return true;

        var session = stripeEvent.Data.Object as Session;
        if (session?.Id == null)
            return true;

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.StripeSessionId == session.Id, ct);
        if (payment == null)
        {
            _log.LogWarning("Webhook: no payment for session {SessionId}", session.Id);
            return true;
        }

        payment.Status = PaymentStatus.Completed;
        payment.CompletedAt = DateTime.UtcNow;
        payment.StripePaymentIntentId = session.PaymentIntentId ?? session.PaymentIntent?.Id;
        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Payment {Id} completed for placement {PlacementId}", payment.Id, payment.PlacementId);
        return true;
    }

    public async Task<PaymentStatusDto?> GetStatusAsync(int paymentId, int? applicantId, bool canViewPaymentDetails, CancellationToken ct = default)
    {
        var p = await _db.Payments
            .Include(x => x.Animal)
            .FirstOrDefaultAsync(x => x.Id == paymentId, ct);
        if (p == null)
            return null;
        if (applicantId.HasValue && p.ApplicantId != applicantId.Value)
            return null;

        return new PaymentStatusDto(
            p.Id,
            p.Animal.PublicId,
            p.Animal.Name,
            p.Amount,
            p.Status.ToString(),
            p.CreatedAt,
            p.CompletedAt,
            canViewPaymentDetails ? p.StripePaymentIntentId : null);
    }

    public async Task<IReadOnlyList<PaymentStatusDto>> ListByApplicantAsync(int applicantId, bool canViewPaymentDetails, CancellationToken ct = default)
    {
        var list = await _db.Payments
            .Include(p => p.Animal)
            .Where(p => p.ApplicantId == applicantId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return list.Select(p => new PaymentStatusDto(
            p.Id,
            p.Animal.PublicId,
            p.Animal.Name,
            p.Amount,
            p.Status.ToString(),
            p.CreatedAt,
            p.CompletedAt,
            canViewPaymentDetails ? p.StripePaymentIntentId : null)).ToList();
    }

    public async Task<bool> IsPlacementPaidAsync(int placementId, CancellationToken ct = default)
        => await _db.Payments.AnyAsync(p => p.PlacementId == placementId && p.Status == PaymentStatus.Completed, ct);

    public async Task<bool> HasPendingPaymentAsync(int placementId, CancellationToken ct = default)
        => await _db.Payments.AnyAsync(p => p.PlacementId == placementId && p.Status == PaymentStatus.Pending, ct);

    public async Task<HashSet<int>> GetPaidPlacementIdsAsync(IEnumerable<int> placementIds, CancellationToken ct = default)
    {
        var ids = placementIds.ToList();
        if (ids.Count == 0) return new HashSet<int>();
        var paid = await _db.Payments
            .Where(p => p.PlacementId != null && ids.Contains(p.PlacementId.Value) && p.Status == PaymentStatus.Completed)
            .Select(p => p.PlacementId!.Value)
            .ToListAsync(ct);
        return paid.ToHashSet();
    }

    public async Task<PaymentStatusDto?> GetByPlacementAsync(int placementId, bool canViewPaymentDetails, CancellationToken ct = default)
    {
        var p = await _db.Payments
            .Include(x => x.Animal)
            .FirstOrDefaultAsync(x => x.PlacementId == placementId, ct);
        if (p == null) return null;
        return new PaymentStatusDto(
            p.Id, p.Animal.PublicId, p.Animal.Name, p.Amount, p.Status.ToString(),
            p.CreatedAt, p.CompletedAt, canViewPaymentDetails ? p.StripePaymentIntentId : null);
    }
}
