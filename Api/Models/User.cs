using System.ComponentModel.DataAnnotations;

namespace AdoptionAgency.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(256)]
    public string Email { get; set; } = null!;

    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = null!;

    public UserRole Role { get; set; }

    /// <summary>Employee-only: can view payment details. Default false.</summary>
    public bool CanViewPaymentDetails { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public Applicant? Applicant { get; set; }
}
