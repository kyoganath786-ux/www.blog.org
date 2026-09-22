using NickAI.Core.Abstractions;
using NickAI.Core.Models;

namespace NickAI.Core.Tests.Fakes;

/// <summary>An agent stub that records execution and optionally publishes an artifact.</summary>
internal sealed class FakeAgent : IAgent
{
    private readonly string? _artifactName;

    public FakeAgent(string capability, string? artifactName = null, Exception? failWith = null)
    {
        Capability = capability;
        _artifactName = artifactName;
        FailWith = failWith;
    }

    public string Name => $"{Capability}Agent";

    public string Capability { get; }

    public Exception? FailWith { get; }

    public int Executions { get; private set; }

    public Task<AgentTask> ExecuteAsync(AgentTask task, AgentContext context, CancellationToken ct = default)
    {
        Executions++;

        if (FailWith is not null) throw FailWith;

        if (_artifactName is not null)
        {
            context.Artifacts.CreateFile(_artifactName, ArtifactType.Text, "generated content");
        }

        task.Status = AgentTaskStatus.Succeeded;
        task.Result = "ok";
        return Task.FromResult(task);
    }
}
