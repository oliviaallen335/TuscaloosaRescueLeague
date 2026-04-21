using System.ComponentModel.DataAnnotations;

namespace AdoptionAgency.Api.Models;

public class Animal
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string PublicId { get; set; } = null!;

    public Species Species { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public Sex Sex { get; set; }

    [MaxLength(20)]
    public string? Size { get; set; }

    [MaxLength(50)]
    public string? Color { get; set; }

    public int? EstimatedAgeMonths { get; set; }

    [MaxLength(100)]
    public string? BreedPrimary { get; set; }

    [MaxLength(100)]
    public string? BreedSecondary { get; set; }

    public AnimalStatus Status { get; set; }

    public DateTime IntakeDate { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AnimalPhoto> Photos { get; set; } = new List<AnimalPhoto>();
    public BehaviorProfile? BehaviorProfile { get; set; }
    public ICollection<Placement> Placements { get; set; } = new List<Placement>();
    public ICollection<Intake> Intakes { get; set; } = new List<Intake>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
