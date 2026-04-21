using System.ComponentModel.DataAnnotations;

namespace AdoptionAgency.Api.Models;

public record ApplicationListDto(
    int Id,
    int ApplicantId,
    string ApplicantName,
    string AnimalPublicId,
    string AnimalName,
    ApplicationStatus Status,
    DateTime SubmittedAt,
    DateTime? ReviewedAt);

public record ApplicationDetailDto(
    int Id,
    int ApplicantId,
    string ApplicantName,
    string ApplicantEmail,
    string AnimalPublicId,
    string AnimalName,
    ApplicationStatus Status,
    DateTime SubmittedAt,
    DateTime? ReviewedAt,
    string? Notes,
    string? AnswersJson);

public class ApplicationCreateDto
{
    [Required, MaxLength(20)]
    public string AnimalPublicId { get; set; } = null!;
    public string? AnswersJson { get; set; }
}

public class ApplicationReviewDto
{
    public ApplicationStatus Status { get; set; }
    public string? Notes { get; set; }
}
