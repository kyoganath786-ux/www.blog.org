using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using NickAI.Core.Abstractions;
using NickAI.Core.Models;
using Microsoft.Extensions.Logging;

namespace NickAI.Core.Providers;

/// <summary>
/// Talks to a local Ollama runtime over HTTP (default http://localhost:11434).
/// Capabilities come from <c>/api/show</c> when Ollama reports them, otherwise
/// from conservative name heuristics. Unknown models are Completion-only.
/// </summary>
public sealed class OllamaModelProvider : IModelProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<OllamaModelProvider>? _logger;

    public OllamaModelProvider(string baseUrl = "http://localhost:11434", ILogger<OllamaModelProvider>? logger = null)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = Timeout.InfiniteTimeSpan };
        _logger = logger;
    }

    public string Name => "ollama";

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync("api/tags", ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Ollama not reachable.");
            return false;
        }
    }

    public async Task<IReadOnlyList<ModelInfo>> ListModelsAsync(CancellationToken ct = default)
    {
        var models = new List<ModelInfo>();
        try
        {
            using var response = await _http.GetAsync("api/tags", ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return models;

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("models", out var arr)) return models;

            foreach (var m in arr.EnumerateArray())
            {
                var name = m.TryGetProperty("name", out var n) ? n.GetString() : null;
                if (string.IsNullOrWhiteSpace(name)) continue;

                string? family = null;
                string? parameterSize = null;
                string? quantization = null;
                if (m.TryGetProperty("details", out var details))
                {
                    if (details.TryGetProperty("family", out var familyElement)) family = familyElement.GetString();
                    if (details.TryGetProperty("parameter_size", out var sizeElement)) parameterSize = sizeElement.GetString();
                    if (details.TryGetProperty("quantization_level", out var quantElement)) quantization = quantElement.GetString();
                }

                long sizeBytes = 0;
                if (m.TryGetProperty("size", out var sizeProperty) && sizeProperty.TryGetInt64(out var parsedSize))
                    sizeBytes = parsedSize;

                var info = new ModelInfo
                {
                    Name = name,
                    Family = family,
                    ParameterSize = parameterSize,
                    Quantization = quantization,
                    SizeBytes = sizeBytes,
                };
                info.Capabilities = await ResolveCapabilitiesAsync(info, ct).ConfigureAwait(false);
                models.Add(info);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to list Ollama models.");
        }

        return models.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<ModelCapabilities> ResolveCapabilitiesAsync(ModelInfo info, CancellationToken ct)
    {
        // Prefer the runtime's own capability report.
        try
        {
            using var response = await _http.PostAsJsonAsync("api/show", new { model = info.Name }, ct).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
                if (doc.RootElement.TryGetProperty("capabilities", out var caps) && caps.ValueKind == JsonValueKind.Array)
                {
                    var result = ModelCapabilities.None;
                    foreach (var c in caps.EnumerateArray())
                    {
                        result |= (c.GetString()?.ToLowerInvariant()) switch
                        {
                            "completion" => ModelCapabilities.Completion,
                            "tools" => ModelCapabilities.Tools,
                            "vision" => ModelCapabilities.Vision,
                            "embedding" => ModelCapabilities.Embedding,
                            _ => ModelCapabilities.None,
                        };
                    }
                    if (result != ModelCapabilities.None) return result;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Ollama /api/show failed for {Model}.", info.Name);
        }

        // Fall back to conservative heuristics; never over-claim.
        return GuessCapabilities(info.Name, info.Family);
    }

    internal static ModelCapabilities GuessCapabilities(string name, string? family)
    {
        var key = $"{name} {family}".ToLowerInvariant();
        var caps = ModelCapabilities.Completion;

        if (key.Contains("embed")) caps = ModelCapabilities.Embedding;
        if (key.Contains("llava") || key.Contains("bakllava") || key.Contains("moondream") || key.Contains("vision"))
            caps |= ModelCapabilities.Vision;
        if (key.Contains("tools") || key.Contains("functionary") || key.Contains("hermes"))
            caps |= ModelCapabilities.Tools;

        return caps;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(ModelRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var body = BuildChatBody(request, stream: true);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"Ollama returned {(int)response.StatusCode}: {error}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (true)
        {
            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null) break;
            if (line.Length == 0) continue;

            string? delta = null;
            var done = false;
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    delta = content.GetString();
                }
                if (doc.RootElement.TryGetProperty("done", out var d) && d.ValueKind == JsonValueKind.True)
                {
                    done = true;
                }
            }
            catch (JsonException ex)
            {
                _logger?.LogDebug(ex, "Skipping malformed Ollama stream line.");
            }

            if (!string.IsNullOrEmpty(delta)) yield return delta;
            if (done) break;
        }
    }

    public async Task<ModelResult> CompleteAsync(ModelRequest request, CancellationToken ct = default)
    {
        var start = DateTimeOffset.Now;
        var body = BuildChatBody(request, stream: false);

        using var response = await _http.PostAsJsonAsync("api/chat", body, ct).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama returned {(int)response.StatusCode}: {payload}");

        using var doc = JsonDocument.Parse(payload);
        var text = doc.RootElement.TryGetProperty("message", out var message) &&
                   message.TryGetProperty("content", out var content)
            ? content.GetString() ?? string.Empty
            : string.Empty;

        return new ModelResult(text, request.Model, DateTimeOffset.Now - start);
    }

    private static Dictionary<string, object?> BuildChatBody(ModelRequest request, bool stream)
    {
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new { role = "system", content = request.SystemPrompt });

        foreach (var m in request.Messages)
        {
            messages.Add(new { role = RoleKey(m.Role), content = m.Content });
        }

        var options = new Dictionary<string, object?>
        {
            ["temperature"] = request.Temperature,
        };
        if (request.ContextSize > 0) options["num_ctx"] = request.ContextSize;
        if (request.MaxTokens > 0) options["num_predict"] = request.MaxTokens;

        return new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["messages"] = messages,
            ["stream"] = stream,
            ["options"] = options,
        };
    }

    private static string RoleKey(ChatRole role) => role switch
    {
        ChatRole.System => "system",
        ChatRole.User => "user",
        ChatRole.Assistant => "assistant",
        ChatRole.Tool => "tool",
        _ => "user",
    };
}
