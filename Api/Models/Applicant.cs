using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdoptionAgency.Api.Models;

public class Applicant
{
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = null!;

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? AddressLine1 { get; set; }

    [MaxLength(200)]
    public string? AddressLine2 { get; set; }

    [MaxLength(50)]
    public string? City { get; set; }

    [MaxLength(20)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    public bool? HasKids { get; set; }
    public bool? HasCats { get; set; }
    public bool? HasDogs { get; set; }
    public bool? HasYard { get; set; }

    [MaxLength(50)]
    public string? HousingType { get; set; }

    [MaxLength(50)]
    public string? ExperienceLevel { get; set; }

    /// <summary>Last JSON submitted from Find Your Match (audit / re-run).</summary>
    public string? MatchQuestionnaireJson { get; set; }

    public DateTime? LastMatchRunAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Placement> Placements { get; set; } = new List<Placement>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
