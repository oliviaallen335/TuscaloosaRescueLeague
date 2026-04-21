using System.Text.Json;
using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Services;

public class MatchingService
{
    private readonly AdoptionDbContext _db;
    private readonly DeepSeekNarrativeService _narrative;

    public MatchingService(AdoptionDbContext db, DeepSeekNarrativeService narrative)
    {
        _db = db;
        _narrative = narrative;
    }

    public static QuestionnaireSchemaDto QuestionnaireSchema { get; } = new(
        new QuestionnaireFieldDto[]
        {
            new("preferredSpecies", "Preferred species", "select",
                new[] { "Any", "Dog", "Cat" },
                "Leave Any if you are open to both."),
            new("maxEnergyComfort", "How much daily exercise can you offer?", "select",
                new[] { "Unknown", "Low", "Medium", "High" },
                "Higher energy pets need more walks and stimulation."),
            new("hasKids", "Kids in the home?", "select", new[] { "Use profile", "Yes", "No" }, null),
            new("kidsAreAnimalSavvy", "If kids are in the home, are they already gentle with animals?", "select",
                new[] { "Unknown", "Yes", "No" }, null),
            new("hasCats", "Cats in the home?", "select", new[] { "Use profile", "Yes", "No" }, null),
            new("hasDogs", "Dogs in the home?", "select", new[] { "Use profile", "Yes", "No" }, null),
            new("hasLargeSpace", "Do you have space for a large animal?", "select", new[] { "Unknown", "Yes", "No" }, null),
            new("housingSituation", "Living situation", "select",
                new[] { "Use profile", "Apartment", "House", "Other" },
                "Apartment renters: check lease rules for pets."),
            new("experienceLevel", "Pet experience", "select",
                new[] { "Use profile", "First-time", "Some experience", "Very experienced" }, null),
            new("dailyActivityLevel", "Your household activity level", "select",
                new[] { "Unknown", "Low", "Medium", "High" }, "Used in vector scoring against pet energy."),
            new("noiseTolerance", "Noise tolerance at home", "select",
                new[] { "Unknown", "Low", "Medium", "High" }, null)
        });

    public async Task<MatchRunResultDto?> RunAsync(int applicantId, MatchRunDto request, CancellationToken ct = default)
    {
        var applicant = await _db.Applicants.FirstOrDefaultAsync(a => a.Id == applicantId, ct);
        if (applicant == null)
            return null;

        var answers = request.Answers ?? new MatchAnswersDto();
        var ctx = MergeContext(applicant, answers);

        if (request.Answers != null)
        {
            applicant.MatchQuestionnaireJson = JsonSerializer.Serialize(request.Answers);
            applicant.LastMatchRunAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        var animals = await _db.Animals
            .Include(a => a.Photos)
            .Include(a => a.BehaviorProfile)
            .Where(a => a.Status == AnimalStatus.Adoptable || a.Status == AnimalStatus.InFoster)
            .OrderByDescending(a => a.IntakeDate)
            .Take(150)
            .ToListAsync(ct);

        var scored = new List<ScoredMatchDto>();
        foreach (var animal in animals)
        {
            var (match, dto) = ScoreOne(animal, ctx);
            if (match && dto != null)
                scored.Add(dto);
        }

        var ordered = scored.OrderByDescending(x => x.Score).Take(5).ToList();

        string? narrative = null;
        string? note = null;
        if (ordered.Count > 0)
        {
            var household = new
            {
                ctx.HasKids,
                ctx.HasCats,
                ctx.HasDogs,
                PreferredSpecies = ctx.PreferredSpecies?.ToString(),
                ctx.MaxEnergyComfort,
                ctx.HousingSituation,
                ctx.ExperienceLevel
            };
            narrative = await _narrative.TryBuildMatchNarrativeAsync(household, ordered.Take(8).ToList(), ct);
            if (narrative == null)
                note = "Narrative skipped: set DeepSeek:ApiKey (see README-SECRETS.md) or check API errors in logs.";
        }

        return new MatchRunResultDto(ordered, narrative, note);
    }

    /// <summary>Lightweight list for GET /me/matches (no narrative, top 25).</summary>
    public async Task<IReadOnlyList<ScoredMatchDto>> ListPreviewAsync(int applicantId, CancellationToken ct = default)
    {
        var result = await RunAsync(applicantId, new MatchRunDto { Answers = null, IncludeNarrative = false }, ct);
        return result?.Matches ?? Array.Empty<ScoredMatchDto>();
    }

    private static MatchContext MergeContext(Applicant a, MatchAnswersDto q) =>
        new(
            HasKids: q.HasKids ?? a.HasKids,
            HasCats: q.HasCats ?? a.HasCats,
            HasDogs: q.HasDogs ?? a.HasDogs,
            KidsAreAnimalSavvy: q.KidsAreAnimalSavvy,
            HasLargeSpace: q.HasLargeSpace,
            PreferredSpecies: q.PreferredSpecies,
            MaxEnergyComfort: q.MaxEnergyComfort,
            HousingSituation: PickOrProfile(q.HousingSituation, a.HousingType),
            ExperienceLevel: PickOrProfile(q.ExperienceLevel, a.ExperienceLevel),
            DailyActivityLevel: q.DailyActivityLevel,
            NoiseTolerance: q.NoiseTolerance);

    private static string? PickOrProfile(string? fromForm, string? profile)
    {
        if (string.IsNullOrWhiteSpace(fromForm) || fromForm.Equals("Use profile", StringComparison.OrdinalIgnoreCase))
            return profile;
        return fromForm;
    }

    private static (bool Included, ScoredMatchDto? Dto) ScoreOne(Animal animal, MatchContext ctx)
    {
        var breakdown = new List<string>();
        var reasons = new List<string>();
        var bp = animal.BehaviorProfile;

        if (ctx.PreferredSpecies.HasValue && animal.Species != ctx.PreferredSpecies.Value)
            return (false, null);

        if (ctx.HasCats == true)
        {
            if (bp?.GoodWithCats == GoodWith.No)
                return (false, null);
            if (bp == null || bp.GoodWithCats == GoodWith.Unknown)
            {
                breakdown.Add("-8: cat compatibility unknown");
                reasons.Add("Cat compatibility not yet assessed by staff.");
            }
        }

        if (ctx.HasDogs == true)
        {
            if (bp?.GoodWithDogs == GoodWith.No)
                return (false, null);
            if (bp == null || bp.GoodWithDogs == GoodWith.Unknown)
            {
                breakdown.Add("-8: dog compatibility unknown");
                reasons.Add("Dog compatibility not yet assessed.");
            }
        }

        if (ctx.HasKids == true)
        {
            if (bp?.GoodWithKids == GoodWith.No)
                return (false, null);
            if (bp == null || bp.GoodWithKids == GoodWith.Unknown)
            {
                breakdown.Add("-8: child compatibility unknown");
                reasons.Add("Child compatibility not yet assessed.");
            }
        }

        // Build dense vectors and use cosine similarity ("vector search" style ranking).
        var applicantVec = BuildApplicantVector(ctx);
        var animalVec = BuildAnimalVector(animal, bp);
        var cosine = CosineSimilarity(applicantVec, animalVec); // 0..1-ish for non-negative vectors
        var score = (int)Math.Round(Math.Clamp(cosine, 0d, 1d) * 100d);
        breakdown.Add($"+{score}: vector similarity score");

        if (animal.Status == AnimalStatus.Adoptable)
        {
            score += 5;
            breakdown.Add("+5: adoptable now");
        }

        var animalEnergy = bp?.EnergyLevel ?? EnergyLevel.Unknown;
        if (ctx.MaxEnergyComfort.HasValue && animalEnergy != EnergyLevel.Unknown && (int)animalEnergy > (int)ctx.MaxEnergyComfort.Value)
        {
            score -= 10;
            breakdown.Add("-10: energy above comfort range");
            reasons.Add($"Energy level {animalEnergy} may be above your selected comfort level.");
        }

        if (ctx.HasKids == true && ctx.KidsAreAnimalSavvy == false && animalEnergy == EnergyLevel.High)
        {
            score -= 8;
            breakdown.Add("-8: high-energy pet with kids new to animals");
            reasons.Add("Because kids are still getting used to pets, calmer animals may be easier.");
        }

        if (ctx.HasLargeSpace == false && IsLargeSpaceAnimal(animal, bp))
        {
            score -= 12;
            breakdown.Add("-12: limited space for large/high-energy pet");
            reasons.Add("This pet may do better in a larger space.");
        }

        if (reasons.Count == 0)
            reasons.Add("Vector match indicates a strong fit for your household profile.");

        var primary = animal.Photos.OrderBy(p => p.DisplayOrder).FirstOrDefault(p => p.IsPrimary)
            ?? animal.Photos.OrderBy(p => p.DisplayOrder).FirstOrDefault();

        return (true, new ScoredMatchDto(
            animal.PublicId,
            animal.Name,
            animal.Species,
            animal.BreedPrimary,
            primary?.FilePath,
            Math.Clamp(score, 0, 100),
            reasons,
            breakdown));
    }

    private static double[] BuildApplicantVector(MatchContext ctx)
    {
        var prefDog = ctx.PreferredSpecies switch { Species.Dog => 1d, Species.Cat => 0d, _ => 0.6d };
        var prefCat = ctx.PreferredSpecies switch { Species.Cat => 1d, Species.Dog => 0d, _ => 0.6d };
        return new[]
        {
            prefDog, // species: dog
            prefCat, // species: cat
            BoolTo01(ctx.HasKids),
            BoolTo01(ctx.HasCats),
            BoolTo01(ctx.HasDogs),
            BoolTo01(ctx.KidsAreAnimalSavvy),
            BoolTo01(ctx.HasLargeSpace),
            EnergyTo01(ctx.MaxEnergyComfort),
            ActivityTo01(ctx.DailyActivityLevel),
            ExperienceTo01(ctx.ExperienceLevel),
            NoiseTo01(ctx.NoiseTolerance)
        };
    }

    private static double[] BuildAnimalVector(Animal animal, BehaviorProfile? bp)
    {
        var isDog = animal.Species == Species.Dog ? 1d : 0d;
        var isCat = animal.Species == Species.Cat ? 1d : 0d;
        var energy = EnergyTo01(bp?.EnergyLevel);
        return new[]
        {
            isDog,
            isCat,
            GoodWithTo01(bp?.GoodWithKids),
            GoodWithTo01(bp?.GoodWithCats),
            GoodWithTo01(bp?.GoodWithDogs),
            GoodWithTo01(bp?.GoodWithKids), // proxy for tolerance with less experienced kids
            IsLargeSpaceAnimal(animal, bp) ? 1d : 0.2d,
            energy,
            energy, // active household match
            RequiredExperienceTo01(bp?.EnergyLevel),
            NoiseNeedTo01(bp?.EnergyLevel)
        };
    }

    private static bool IsLargeSpaceAnimal(Animal animal, BehaviorProfile? bp)
    {
        var size = animal.Size?.ToLowerInvariant() ?? "";
        return size.Contains("large") || (bp?.EnergyLevel == EnergyLevel.High);
    }

    private static double CosineSimilarity(double[] a, double[] b)
    {
        if (a.Length != b.Length) return 0d;
        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        if (magA == 0 || magB == 0) return 0d;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    private static double BoolTo01(bool? value) => value switch { true => 1d, false => 0d, _ => 0.5d };
    private static double GoodWithTo01(GoodWith? value) => value switch { GoodWith.Yes => 1d, GoodWith.No => 0d, _ => 0.5d };
    private static double EnergyTo01(EnergyLevel? value) => value switch { EnergyLevel.Low => 0.33d, EnergyLevel.Medium => 0.66d, EnergyLevel.High => 1d, _ => 0.5d };
    private static double ActivityTo01(string? value) => ParseBand(value);
    private static double NoiseTo01(string? value) => ParseBand(value);
    private static double RequiredExperienceTo01(EnergyLevel? value) => value switch { EnergyLevel.High => 0.9d, EnergyLevel.Medium => 0.6d, EnergyLevel.Low => 0.3d, _ => 0.5d };
    private static double NoiseNeedTo01(EnergyLevel? value) => value switch { EnergyLevel.High => 0.9d, EnergyLevel.Medium => 0.6d, EnergyLevel.Low => 0.3d, _ => 0.5d };
    private static double ExperienceTo01(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0.5d;
        if (value.Contains("first", StringComparison.OrdinalIgnoreCase)) return 0.25d;
        if (value.Contains("very", StringComparison.OrdinalIgnoreCase)) return 1d;
        if (value.Contains("some", StringComparison.OrdinalIgnoreCase) || value.Contains("experience", StringComparison.OrdinalIgnoreCase)) return 0.65d;
        return 0.5d;
    }
    private static double ParseBand(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("Unknown", StringComparison.OrdinalIgnoreCase)) return 0.5d;
        if (value.Equals("Low", StringComparison.OrdinalIgnoreCase)) return 0.33d;
        if (value.Equals("Medium", StringComparison.OrdinalIgnoreCase)) return 0.66d;
        if (value.Equals("High", StringComparison.OrdinalIgnoreCase)) return 1d;
        return 0.5d;
    }

    private sealed record MatchContext(
        bool? HasKids,
        bool? HasCats,
        bool? HasDogs,
        bool? KidsAreAnimalSavvy,
        bool? HasLargeSpace,
        Species? PreferredSpecies,
        EnergyLevel? MaxEnergyComfort,
        string? HousingSituation,
        string? ExperienceLevel,
        string? DailyActivityLevel,
        string? NoiseTolerance);
}
