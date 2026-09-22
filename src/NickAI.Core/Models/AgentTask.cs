namespace NickAI.Core.Models;

public enum AgentTaskStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Cancelled,
}

/// <summary>One unit of work produced by the planner and executed by an agent.</summary>
public sealed class AgentTask
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");

    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Capability required to run this task, e.g. "code", "image", "presentation".</summary>
    public string Capability { get; set; } = "chat";

    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Pending;

    public string? Result { get; set; }

    public string? Error { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>A multi-step plan for a user request.</summary>
public sealed class Plan
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");

    public required string Goal { get; init; }

    /// <summary>Concise, user-visible summary of the plan. No private chain-of-thought.</summary>
    public string Summary { get; set; } = string.Empty;

    public List<AgentTask> Steps { get; } = new();
}
