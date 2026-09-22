using NickAI.Core.Abstractions;
using NickAI.Core.Chat;
using NickAI.Core.Models;
using NickAI.Core.Planning;

namespace NickAI.Core.Agents;

/// <summary>
/// Generates code with the active model and writes it to a real file artifact.
/// Language/extension is derived from the request and the generated fence tag.
/// </summary>
public sealed class CodeAgent : IAgent
{
    private static readonly Dictionary<string, string> LanguageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cs"] = ".cs", ["csharp"] = ".cs", ["c#"] = ".cs",
        ["py"] = ".py", ["python"] = ".py",
        ["js"] = ".js", ["javascript"] = ".js",
        ["ts"] = ".ts", ["typescript"] = ".ts", ["tsx"] = ".tsx", ["jsx"] = ".jsx",
        ["java"] = ".java",
        ["cpp"] = ".cpp", ["c++"] = ".cpp", ["c"] = ".c", ["h"] = ".h",
        ["rs"] = ".rs", ["rust"] = ".rs",
        ["go"] = ".go", ["golang"] = ".go",
        ["html"] = ".html", ["css"] = ".css", ["scss"] = ".scss",
        ["sql"] = ".sql",
        ["ps1"] = ".ps1", ["powershell"] = ".ps1",
        ["sh"] = ".sh", ["bash"] = ".sh",
        ["kt"] = ".kt", ["kotlin"] = ".kt",
        ["swift"] = ".swift", ["dart"] = ".dart", ["gd"] = ".gd", ["gdscript"] = ".gd",
        ["json"] = ".json", ["xml"] = ".xml", ["yml"] = ".yml", ["yaml"] = ".yaml",
    };

    public string Name => "CodeAgent";

    public string Capability => Capabilities.Code;

    public async Task<AgentTask> ExecuteAsync(AgentTask task, AgentContext context, CancellationToken ct = default)
    {
        context.Progress?.Report("Writing code...");

        var request = new ModelRequest
        {
            Model = context.Model,
            SystemPrompt = Prompts.Code,
            Messages = new[]
            {
                new ChatMessage { Role = ChatRole.User, Content = task.Description },
            },
        };

        var result = await context.Models.CompleteAsync(request, ct).ConfigureAwait(false);
        var code = MessageContentParser.FirstCodeBlock(result.Text);

        var extension = ResolveExtension(code?.Language, task.Description);
        var name = $"generated{extension}";
        var artifact = context.Artifacts.CreateFile(
            name,
            ArtifactType.Code,
            code?.Text ?? result.Text,
            context.Project?.Id);

        artifact.Preview = Truncate(code?.Text ?? result.Text, 400);
        artifact.Metadata["language"] = code?.Language ?? "unknown";

        task.Status = AgentTaskStatus.Succeeded;
        task.Result = $"Created {name}";
        return task;
    }

    private static string ResolveExtension(string? fenceLanguage, string description)
    {
        if (!string.IsNullOrWhiteSpace(fenceLanguage) && LanguageExtensions.TryGetValue(fenceLanguage.Trim(), out var byFence))
            return byFence;

        var text = description.ToLowerInvariant();
        foreach (var (key, ext) in LanguageExtensions)
        {
            if (text.Contains(key, StringComparison.OrdinalIgnoreCase))
                return ext;
        }

        return ".txt";
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
