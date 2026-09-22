using NickAI.Core.Abstractions;
using NickAI.Core.Models;
using NickAI.Core.Planning;

namespace NickAI.Core.Agents;

/// <summary>Produces Markdown documentation artifacts.</summary>
public sealed class DocumentationAgent : IAgent
{
    public string Name => "DocumentationAgent";

    public string Capability => Capabilities.Documentation;

    public async Task<AgentTask> ExecuteAsync(AgentTask task, AgentContext context, CancellationToken ct = default)
    {
        context.Progress?.Report("Writing documentation...");

        var request = new ModelRequest
        {
            Model = context.Model,
            SystemPrompt = Prompts.Documentation,
            Messages = new[]
            {
                new ChatMessage { Role = ChatRole.User, Content = task.Description },
            },
        };

        var result = await context.Models.CompleteAsync(request, ct).ConfigureAwait(false);

        var artifact = context.Artifacts.CreateFile(
            "documentation.md",
            ArtifactType.Markdown,
            result.Text,
            context.Project?.Id,
            new Dictionary<string, string> { ["format"] = "markdown" });

        artifact.Preview = result.Text.Length <= 400 ? result.Text : result.Text[..400] + "...";

        task.Status = AgentTaskStatus.Succeeded;
        task.Result = "Created documentation.md";
        return task;
    }
}
