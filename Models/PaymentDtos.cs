namespace AdoptionAgency.Api.Models;

public record PaymentCreateCheckoutRequest(int PlacementId, string SuccessUrl, string CancelUrl);

public record PaymentCreateCheckoutResponse(string? CheckoutUrl, string? Error);

public record PaymentStatusDto(
    int Id,
    string AnimalPublicId,
    string AnimalName,
    decimal Amount,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? StripePaymentIntentId); // Only if CanViewPaymentDetails
