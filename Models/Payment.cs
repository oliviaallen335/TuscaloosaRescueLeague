using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdoptionAgency.Api.Models;

public class Payment
{
    public int Id { get; set; }

    public int AnimalId { get; set; }
    [ForeignKey(nameof(AnimalId))]
    public Animal Animal { get; set; } = null!;

    public int ApplicantId { get; set; }
    [ForeignKey(nameof(ApplicantId))]
    public Applicant Applicant { get; set; } = null!;

    public int? PlacementId { get; set; }
    [ForeignKey(nameof(PlacementId))]
    public Placement? Placement { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Stripe Checkout Session ID (for redirect flow).</summary>
    [MaxLength(128)]
    public string? StripeSessionId { get; set; }

    /// <summary>Set when webhook confirms payment. No card storage.</summary>
    [MaxLength(128)]
    public string? StripePaymentIntentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
