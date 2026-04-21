using System.ComponentModel.DataAnnotations;

namespace AdoptionAgency.Api.Models;

public record LoginRequest([Required] string Email, [Required] string Password);

public record LoginResponse(string Token, string Email, string Role, int UserId, int? ApplicantId);

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName);

public record MeResponse(string Email, string Role, int UserId, int? ApplicantId, bool CanViewPaymentDetails);
