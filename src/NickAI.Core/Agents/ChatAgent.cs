using NickAI.Core.Abstractions;
using NickAI.Core.Models;
using NickAI.Core.Planning;

namespace NickAI.Core.Agents;

/// <summary>Answers a request directly with the selected model.</summary>
public sealed class ChatAgent : IAgent
{
    public string Name => "ChatAgent";

    public string Capability => Capabilities.Chat;

    public async Task<AgentTask> ExecuteAsync(AgentTask task, AgentContext context, CancellationToken ct = default)
    {
        var messages = context.Conversation.Messages
            .Where(m => m.Role is ChatRole.User or ChatRole.Assistant)
            .Select(m => new ChatMessage { Role = m.Role, Content = m.Content })
            .ToList();

        var request = new ModelRequest
        {
            Model = context.Model,
            Messages = messages,
            SystemPrompt = Prompts.Assistant,
        };

        var result = await context.Models.CompleteAsync(request, ct).ConfigureAwait(false);
        task.Status = AgentTaskStatus.Succeeded;
        task.Result = result.Text;
        return task;
    }
}
