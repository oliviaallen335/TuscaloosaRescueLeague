using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AdoptionAgency.Api.Data;
using AdoptionAgency.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptionAgency.Api.Services;

/// <summary>One name at a time via DeepSeek. No key = null. Always excludes existing animal names.</summary>
public class NameSuggestionService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly AdoptionDbContext _db;
    private readonly ILogger<NameSuggestionService> _log;

    public NameSuggestionService(IHttpClientFactory httpFactory, IConfiguration config, AdoptionDbContext db, ILogger<NameSuggestionService> log)
    {
        _httpFactory = httpFactory;
        _config = config;
        _db = db;
        _log = log;
    }

    public async Task<string?> SuggestOneAsync(Species species, Sex sex, string? color, string? breed, string? excludeAnimalPublicId = null, CancellationToken ct = default)
    {
        var apiKey = _config["DeepSeek:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var q = _db.Animals.AsQueryable();
        if (!string.IsNullOrWhiteSpace(excludeAnimalPublicId))
            q = q.Where(a => a.PublicId != excludeAnimalPublicId);
        var existing = await q.Select(a => a.Name.ToLower()).ToListAsync(ct);
        var exclude = existing.Count > 0 ? string.Join(", ", existing.Take(200)) : "";

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var prompt = BuildPrompt(species, sex, color, breed, exclude, attempt > 0);
            var name = await CallDeepSeekAsync(prompt, apiKey, ct);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            name = NormalizeName(name);
            if (name.Length < 2 || name.Length > 50)
                continue;

            var exists = existing.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
            if (!exists)
                return name;

            exclude += ", " + name;
        }

        return null;
    }

    private static string BuildPrompt(Species species, Sex sex, string? color, string? breed, string exclude, bool retry)
    {
        var sb = new StringBuilder();
        sb.Append("Suggest ONE unique pet name for a ");
        sb.Append(species == Species.Dog ? "dog" : "cat");
        if (sex != Sex.Unknown)
            sb.Append(sex == Sex.Male ? " male" : " female");
        if (!string.IsNullOrWhiteSpace(color))
            sb.Append(", ").Append(color);
        if (!string.IsNullOrWhiteSpace(breed))
            sb.Append(", ").Append(breed);
        sb.Append(". ");
        if (retry)
            sb.Append("The previous suggestion was already taken. ");
        sb.Append("Return ONLY the name—no quotes, punctuation, or explanation. ");
        sb.Append("Be creative but recognizable. ");
        if (!string.IsNullOrWhiteSpace(exclude))
            sb.Append("Do NOT use any of these (already in our shelter): ").Append(exclude).Append(". ");
        sb.Append("One word or short phrase only.");
        return sb.ToString();
    }

    private async Task<string?> CallDeepSeekAsync(string prompt, string apiKey, CancellationToken ct)
    {
        var baseUrl = (_config["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com").TrimEnd('/');
        var model = _config["DeepSeek:Model"] ?? "deepseek-chat";
        var url = baseUrl.Contains("/v1", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUrl}/chat/completions"
            : $"{baseUrl}/v1/chat/completions";

        var payload = new
        {
            model,
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = 30
        };

        try
        {
            var client = _httpFactory.CreateClient("DeepSeek");
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var resp = await client.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _log.LogWarning("Name suggest HTTP {Status}", (int)resp.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return content;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Name suggest failed");
            return null;
        }
    }

    private static string NormalizeName(string raw)
    {
        var s = raw.Trim();
        foreach (var c in new[] { '"', '\'', '.', '!', '?', '\n', '\r' })
            s = s.Replace(c.ToString(), "");
        var idx = s.IndexOfAny(new[] { ' ', ',', ';', '—', '-' });
        if (idx > 0)
            s = s[..idx];
        return s.Trim();
    }
}
