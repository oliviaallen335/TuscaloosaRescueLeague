using System.ComponentModel.DataAnnotations;

namespace AdoptionAgency.Api.Models;

/// <summary>For applicant self-service: placement + payment status.</summary>
public record PlacementWithPaymentDto(
    int Id,
    string AnimalPublicId,
    string AnimalName,
    PlacementType PlacementType,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? Fee,
    bool IsPaid,
    bool HasPendingPayment = false);

public record PlacementListDto(
    int Id,
    string AnimalPublicId,
    string AnimalName,
    int ApplicantId,
    string ApplicantName,
    PlacementType PlacementType,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? Fee,
    string? Notes,
    bool? IsPaid = null);

public class PlacementCreateDto
{
    [Required, MaxLength(20)]
    public string AnimalPublicId { get; set; } = null!;
    [Range(1, int.MaxValue)]
    public int ApplicantId { get; set; }
    public PlacementType PlacementType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [Range(0, 99999.99)]
    public decimal? Fee { get; set; }
    public string? Notes { get; set; }
}

public record PlacementEndDto(DateTime EndDate);
