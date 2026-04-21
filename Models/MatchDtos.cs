using System.Text.Json.Serialization;

namespace AdoptionAgency.Api.Models;

/// <summary>Answers from "Find your match" form. Nulls = use saved applicant profile where applicable.</summary>
public class MatchAnswersDto
{
    /// <summary>null = any species</summary>
    public Species? PreferredSpecies { get; set; }

    /// <summary>Max energy level the household is comfortable with (animal above this is penalized).</summary>
    public EnergyLevel? MaxEnergyComfort { get; set; }

    public bool? HasKids { get; set; }
    public bool? HasCats { get; set; }
    public bool? HasDogs { get; set; }
    public bool? KidsAreAnimalSavvy { get; set; }
    public bool? HasLargeSpace { get; set; }

    /// <summary>e.g. apartment, house — affects high-energy pets</summary>
    public string? HousingSituation { get; set; }

    /// <summary>first-time, experienced</summary>
    public string? ExperienceLevel { get; set; }
    public string? DailyActivityLevel { get; set; }
    public string? NoiseTolerance { get; set; }
}

public class MatchRunDto
{
    public MatchAnswersDto? Answers { get; set; }

    /// <summary>If true and DeepSeek:ApiKey is set, append an AI narrative (extra latency/cost).</summary>
    public bool IncludeNarrative { get; set; }
}

public record ScoredMatchDto(
    string PublicId,
    string Name,
    Species Species,
    string? BreedPrimary,
    string? PrimaryPhotoUrl,
    int Score,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> ScoreBreakdown);

public record MatchRunResultDto(
    IReadOnlyList<ScoredMatchDto> Matches,
    string? Narrative,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? NarrativeNote);

/// <summary>Static questionnaire for GET /api/match/questionnaire</summary>
public record QuestionnaireFieldDto(
    string Id,
    string Label,
    string Type,
    IReadOnlyList<string>? Options,
    string? HelpText);

public record QuestionnaireSchemaDto(IReadOnlyList<QuestionnaireFieldDto> Fields);
