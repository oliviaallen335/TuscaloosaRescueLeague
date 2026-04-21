using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AdoptionAgency.Api.Models;

namespace AdoptionAgency.Api.Services;

/// <summary>Optional narrative via DeepSeek OpenAI-compatible API. No key = returns null.</summary>
public class DeepSeekNarrativeService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<DeepSeekNarrativeService> _log;

    public DeepSeekNarrativeService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<DeepSeekNarrativeService> log)
    {
        _httpFactory = httpFactory;
        _config = config;
        _log = log;
    }

    public async Task<string?> TryBuildMatchNarrativeAsync(
        object householdSummary,
        IReadOnlyList<ScoredMatchDto> topMatches,
        CancellationToken ct = default)
    {
        var apiKey = _config["DeepSeek:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var baseUrl = (_config["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com").TrimEnd('/');
        var model = _config["DeepSeek:Model"] ?? "deepseek-chat";

        var sb = new StringBuilder();
        sb.AppendLine("You help an animal shelter. Be concise (under 200 words). No markdown headers.");
        sb.AppendLine("Household summary (JSON): ");
        sb.AppendLine(JsonSerializer.Serialize(householdSummary));
        sb.AppendLine("Top scored animals (do not invent facts; flag unknowns):");
        foreach (var m in topMatches)
        {
            sb.AppendLine($"- {m.Name} ({m.PublicId}, {m.Species}): score {m.Score}. Reasons: {string.Join("; ", m.Reasons)}");
        }

        sb.AppendLine("Task: Explain why the top 3 could fit, what to verify at meet-and-greet, and one caution if any.");

        var client = _httpFactory.CreateClient("DeepSeek");

        var url = baseUrl.Contains("/v1", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUrl}/chat/completions"
            : $"{baseUrl}/v1/chat/completions";

        var payload = new
        {
            model,
            messages = new[] { new { role = "user", content = sb.ToString() } },
            max_tokens = 600
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var resp = await client.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _log.LogWarning("DeepSeek HTTP {Status}: {Body}", (int)resp.StatusCode, body.Length > 500 ? body[..500] : body);
                return null;
            }

            using var doc = JsonDocument.Parse(body);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var content = choice.GetProperty("message").GetProperty("content").GetString();
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "DeepSeek narrative failed");
            return null;
        }
    }
}
