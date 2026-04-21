using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdoptionAgency.Api.Models;

public class Application
{
    public int Id { get; set; }

    public int ApplicantId { get; set; }
    [ForeignKey(nameof(ApplicantId))]
    public Applicant Applicant { get; set; } = null!;

    public int AnimalId { get; set; }
    [ForeignKey(nameof(AnimalId))]
    public Animal Animal { get; set; } = null!;

    public ApplicationStatus Status { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }

    public string? Notes { get; set; }

    /// <summary>JSON or plain text of questionnaire answers.</summary>
    public string? AnswersJson { get; set; }
}
