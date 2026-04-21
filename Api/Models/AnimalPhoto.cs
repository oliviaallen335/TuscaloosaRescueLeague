using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdoptionAgency.Api.Models;

public class AnimalPhoto
{
    public int Id { get; set; }

    public int AnimalId { get; set; }
    [ForeignKey(nameof(AnimalId))]
    public Animal Animal { get; set; } = null!;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public int DisplayOrder { get; set; }
}
