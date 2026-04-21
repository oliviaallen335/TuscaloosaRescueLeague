using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdoptionAgency.Api.Models;

public class BehaviorProfile
{
    public int Id { get; set; }

    public int AnimalId { get; set; }
    [ForeignKey(nameof(AnimalId))]
    public Animal Animal { get; set; } = null!;

    public GoodWith GoodWithKids { get; set; }
    public GoodWith GoodWithCats { get; set; }
    public GoodWith GoodWithDogs { get; set; }
    public GoodWith HouseTrained { get; set; }
    public EnergyLevel EnergyLevel { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
