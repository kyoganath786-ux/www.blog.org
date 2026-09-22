namespace NickAI.Core.Planning;

/// <summary>Result of classifying a user request.</summary>
public sealed record Classification(
    string PrimaryCapability,
    string DisplayName,
    IReadOnlyList<string> CapabilityChain);

/// <summary>
/// Maps a natural-language request to a capability chain. This is intentionally
/// deterministic (keyword rules) so routing never depends on a model guessing
/// JSON correctly. Rules are evaluated in priority order; first match wins.
/// </summary>
public sealed class RequestClassifier
{
    private static readonly (string Capability, string[] Keywords)[] Rules =
    {
        (Capabilities.Presentation, new[] { "powerpoint", "power point", "pptx", "presentation", "slide deck", " slides" }),
        (Capabilities.Spreadsheet, new[] { "excel", "xlsx", "spreadsheet", "financial report", "sales dashboard", "budget sheet" }),
        (Capabilities.Pdf, new[] { "pdf" }),
        (Capabilities.Document, new[] { "word", "docx", "document", "report", "resume", "cv ", "assignment", "essay", "meeting notes", "proposal" }),
        (Capabilities.Game, new[] { "game", "unity", "godot", "unreal" }),
        (Capabilities.Web, new[] { "website", "web site", "web app", "html", "landing page", "webpage" }),
        (Capabilities.App, new[] { "windows app", "desktop app", "android", "mobile app", "wpf", "winui", "maoui", "maui", "flutter", "electron" }),
        (Capabilities.Build, new[] { "exe", "installer", "setup package", "build my", "package my", "publish my", "compile my" }),
        // Development work on existing code. Kept after the artifact rules so
        // "fix my spreadsheet" still routes to the spreadsheet agent.
        (Capabilities.Code, new[] { "fix my code", "fix this code", "fix the code", "fix my project", "c#", "csharp", "refactor", "debug my", "unit test", "source code", ".csproj", ".sln", "compile error", "build error" }),
        (Capabilities.Database, new[] { "database", "sql", "schema", "data model" }),
        (Capabilities.Data, new[] { "csv", "analyze", "analyse", "chart", "dataset", "data analysis" }),
        (Capabilities.Image, new[] { "image", "logo", "icon", "poster", "thumbnail", "illustration", "draw" }),
        (Capabilities.Video, new[] { "video", "animation", "clip" }),
        (Capabilities.Audio, new[] { "voice", "audio", "narration", "text to speech", "tts", "speech", "podcast" }),
        (Capabilities.Design, new[] { "ui/ux", "ux design", "wireframe", "design system", "branding", "typography" }),
        (Capabilities.Research, new[] { "research", "search the web", "browse", "look up", "latest news", "find information" }),
    };

    private static readonly Dictionary<string, string[]> Chains = new(StringComparer.OrdinalIgnoreCase)
    {
        [Capabilities.Chat] = new[] { Capabilities.Chat },
        [Capabilities.Code] = new[] { Capabilities.Code },
        [Capabilities.Documentation] = new[] { Capabilities.Documentation },
        [Capabilities.Web] = new[] { Capabilities.Code },
        [Capabilities.App] = new[] { Capabilities.Code },
        [Capabilities.Database] = new[] { Capabilities.Code },
        [Capabilities.Build] = new[] { Capabilities.Build },
        [Capabilities.Game] = new[] { Capabilities.Game },
        [Capabilities.Presentation] = new[] { Capabilities.Presentation },
        [Capabilities.Spreadsheet] = new[] { Capabilities.Spreadsheet },
        [Capabilities.Document] = new[] { Capabilities.Document },
        [Capabilities.Pdf] = new[] { Capabilities.Pdf },
        [Capabilities.Image] = new[] { Capabilities.Image },
        [Capabilities.Video] = new[] { Capabilities.Video },
        [Capabilities.Audio] = new[] { Capabilities.Audio },
        [Capabilities.Data] = new[] { Capabilities.Data },
        [Capabilities.Research] = new[] { Capabilities.Research },
        [Capabilities.Design] = new[] { Capabilities.Design },
    };

    public Classification Classify(string request)
    {
        var text = request?.ToLowerInvariant() ?? string.Empty;

        foreach (var (capability, keywords) in Rules)
        {
            if (keywords.Any(k => text.Contains(k, StringComparison.OrdinalIgnoreCase)))
                return Build(capability);
        }

        return Build(Capabilities.Chat);
    }

    private static Classification Build(string capability)
    {
        var chain = Chains.TryGetValue(capability, out var c) ? c : new[] { capability };
        return new Classification(capability, Capabilities.Display(capability), chain);
    }
}
