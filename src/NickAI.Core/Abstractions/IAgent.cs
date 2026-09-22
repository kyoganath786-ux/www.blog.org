using NickAI.Core.Models;

namespace NickAI.Core.Abstractions;

/// <summary>Everything an agent needs to execute a task.</summary>
public sealed class AgentContext
{
    public required string Request { get; init; }
    public required Conversation Conversation { get; init; }
    public Project? Project { get; init; }
    public required IModelProvider Models { get; init; }
    public required string Model { get; init; }
    public required IArtifactStore Artifacts { get; init; }

    /// <summary>Concise, user-visible progress ("Writing code...", "Building...").</summary>
    public IProgress<string>? Progress { get; init; }
}

/// <summary>A specialized worker that performs one kind of task.</summary>
public interface IAgent
{
    /// <summary>Display name, e.g. "CodeAgent".</summary>
    string Name { get; }

    /// <summary>Capability key this agent owns, e.g. "code" or "presentation".</summary>
    string Capability { get; }

    /// <summary>Executes the task, returning it with Status/Result/Error updated.</summary>
    Task<AgentTask> ExecuteAsync(AgentTask task, AgentContext context, CancellationToken ct = default);
}
