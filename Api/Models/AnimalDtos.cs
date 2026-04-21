using System.ComponentModel.DataAnnotations;

namespace AdoptionAgency.Api.Models;

public record BehaviorProfileDto(
    GoodWith GoodWithKids,
    GoodWith GoodWithCats,
    GoodWith GoodWithDogs,
    GoodWith HouseTrained,
    EnergyLevel EnergyLevel,
    string? Notes);

public record AnimalListItemDto(
    string PublicId,
    string Name,
    Species Species,
    Sex Sex,
    AnimalStatus Status,
    string? BreedPrimary,
    int? EstimatedAgeMonths,
    string? PrimaryPhotoUrl);

public record AnimalDetailDto(
    string PublicId,
    string Name,
    Species Species,
    Sex Sex,
    string? Size,
    string? Color,
    int? EstimatedAgeMonths,
    string? BreedPrimary,
    string? BreedSecondary,
    AnimalStatus Status,
    DateTime IntakeDate,
    string? Description,
    IReadOnlyList<AnimalPhotoDto> Photos,
    BehaviorProfileDto? Behavior);

public record AnimalPhotoDto(string Url, bool IsPrimary, int DisplayOrder);

public class AnimalCreateDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
    public Species Species { get; set; }
    public Sex Sex { get; set; } = Sex.Unknown;
    public string? Size { get; set; }
    public string? Color { get; set; }
    [Range(0, 600)]
    public int? EstimatedAgeMonths { get; set; }
    public string? BreedPrimary { get; set; }
    public string? BreedSecondary { get; set; }
    public AnimalStatus Status { get; set; } = AnimalStatus.Intake;
    public DateTime? IntakeDate { get; set; }
    public string? Description { get; set; }
    public BehaviorProfileDto? Behavior { get; set; }
    public string? IntakeSource { get; set; }
    public string? IntakeNotes { get; set; }
}

public record AnimalUpdateDto(
    [Required, MaxLength(100)] string Name,
    Species Species,
    Sex Sex,
    string? Size,
    string? Color,
    [Range(0, 600)] int? EstimatedAgeMonths,
    string? BreedPrimary,
    string? BreedSecondary,
    AnimalStatus Status,
    DateTime IntakeDate,
    string? Description,
    BehaviorProfileDto? Behavior);
