using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdoptionAgency.Api.Models;

public class Placement
{
    public int Id { get; set; }

    public int AnimalId { get; set; }
    [ForeignKey(nameof(AnimalId))]
    public Animal Animal { get; set; } = null!;

    public int ApplicantId { get; set; }
    [ForeignKey(nameof(ApplicantId))]
    public Applicant Applicant { get; set; } = null!;

    public PlacementType PlacementType { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Fee { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
