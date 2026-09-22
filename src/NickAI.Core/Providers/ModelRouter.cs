using NickAI.Core.Models;

namespace NickAI.Core.Providers;

/// <summary>
/// Chooses a model for a given capability. Only returns models that actually
/// report the required capability; otherwise falls back to the active model.
/// </summary>
public sealed class ModelRouter
{
    private readonly ModelManager _manager;

    public ModelRouter(ModelManager manager) => _manager = manager;

    public string? Route(string capability)
    {
        var models = _manager.Models;
        if (models.Count == 0) return _manager.ActiveModel;

        var wanted = capability.ToLowerInvariant();

        ModelInfo? pick = wanted switch
        {
            "vision" or "image" => models.FirstOrDefault(m => m.Supports(ModelCapabilities.Vision)),
            "embedding" or "rag" or "knowledge" => models.FirstOrDefault(m => m.Supports(ModelCapabilities.Embedding)),
            "tools" or "browser" or "computer" or "terminal" => models.FirstOrDefault(m => m.Supports(ModelCapabilities.Tools)),
            "code" or "debug" or "build" => models.FirstOrDefault(m => m.Name.Contains("coder", StringComparison.OrdinalIgnoreCase))
                                             ?? models.FirstOrDefault(m => m.Name.Contains("code", StringComparison.OrdinalIgnoreCase)),
            _ => null,
        };

        // Only fall back to a general model for capabilities it can realistically serve.
        pick ??= models.FirstOrDefault(m => m.Name == _manager.ActiveModel);
        pick ??= models.FirstOrDefault(m => m.Supports(ModelCapabilities.Completion));

        return pick?.Name ?? _manager.ActiveModel;
    }
}
