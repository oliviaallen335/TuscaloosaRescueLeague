namespace AdoptionAgency.Api.Models;

public class NameSuggestionRequestDto
{
    public Species Species { get; set; }
    public Sex Sex { get; set; } = Sex.Unknown;
    public string? Color { get; set; }
    public string? BreedPrimary { get; set; }
    /// <summary>When editing, pass current animal publicId so its name is allowed (not excluded).</summary>
    public string? ExcludeAnimalPublicId { get; set; }
}

public record NameSuggestionResponseDto(string? Name, string? Error);
