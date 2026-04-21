using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdoptionAgency.Api.Models;

public class Intake
{
    public int Id { get; set; }

    public int AnimalId { get; set; }
    [ForeignKey(nameof(AnimalId))]
    public Animal Animal { get; set; } = null!;

    [MaxLength(50)]
    public string? Source { get; set; }

    public string? Notes { get; set; }

    public DateTime IntakeDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
