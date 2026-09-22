namespace NickAI.Core.Planning;

/// <summary>Canonical capability keys used to route work to agents.</summary>
public static class Capabilities
{
    public const string Chat = "chat";
    public const string Code = "code";
    public const string Documentation = "documentation";
    public const string Presentation = "presentation";
    public const string Spreadsheet = "spreadsheet";
    public const string Document = "document";
    public const string Pdf = "pdf";
    public const string Image = "image";
    public const string Video = "video";
    public const string Audio = "audio";
    public const string Web = "web";
    public const string App = "app";
    public const string Game = "game";
    public const string Database = "database";
    public const string Data = "data";
    public const string Research = "research";
    public const string Build = "build";
    public const string Design = "design";

    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        [Chat] = "Answering",
        [Code] = "Writing code",
        [Documentation] = "Writing documentation",
        [Presentation] = "Generating presentation",
        [Spreadsheet] = "Generating spreadsheet",
        [Document] = "Generating document",
        [Pdf] = "Generating PDF",
        [Image] = "Creating image",
        [Video] = "Generating video",
        [Audio] = "Generating audio",
        [Web] = "Building website",
        [App] = "Building application",
        [Game] = "Building game",
        [Database] = "Designing database",
        [Data] = "Analyzing data",
        [Research] = "Researching",
        [Build] = "Building",
        [Design] = "Designing",
    };

    public static string Display(string capability) =>
        DisplayNames.TryGetValue(capability, out var name) ? name : capability;
}
