using NickAI.Core.Abstractions;
using NickAI.Core.Models;
using NickAI.Core.Planning;

namespace NickAI.Core.Agents;

/// <summary>Outcome of a full orchestrated run.</summary>
public sealed record OrchestrationResult(Plan Plan, IReadOnlyList<Artifact> Artifacts, string Summary);

/// <summary>
/// Plans a request, dispatches each step to a registered agent, and collects
/// artifacts. Steps whose capability has no agent are marked failed with a
/// truthful message instead of being invented.
/// </summary>
public sealed class OrchestratorAgent
{
    private readonly AgentRegistry _registry;
    private readonly RequestClassifier _classifier;

    public OrchestratorAgent(AgentRegistry registry, RequestClassifier classifier)
    {
        _registry = registry;
        _classifier = classifier;
    }

    public Plan Plan(string request)
    {
        var classification = _classifier.Classify(request);
        var plan = new Plan
        {
            Goal = request,
            Summary = classification.DisplayName,
        };

        var index = 1;
        foreach (var capability in classification.CapabilityChain)
        {
            plan.Steps.Add(new AgentTask
            {
                Title = $"{index++}. {Capabilities.Display(capability)}",
                Capability = capability,
                Description = request,
            });
        }

        return plan;
    }

    public async Task<OrchestrationResult> RunAsync(string request, AgentContext context, CancellationToken ct = default)
    {
        var plan = Plan(request);
        var created = new List<Artifact>();
        void OnArtifactAdded(object? sender, Artifact artifact) => created.Add(artifact);

        context.Artifacts.ArtifactAdded += OnArtifactAdded;
        try
        {
            foreach (var step in plan.Steps)
            {
                ct.ThrowIfCancellationRequested();

                if (!_registry.TryGet(step.Capability, out var agent))
                {
                    step.Status = AgentTaskStatus.Failed;
                    step.Error = $"{Capabilities.Display(step.Capability)} is not implemented in this build.";
                    context.Progress?.Report(step.Error);
                    continue;
                }

                step.Status = AgentTaskStatus.Running;
                step.StartedAt = DateTimeOffset.Now;
                context.Progress?.Report($"{Capabilities.Display(step.Capability)}...");

                try
                {
                    var completed = await agent.ExecuteAsync(step, context, ct).ConfigureAwait(false);
                    step.Status = completed.Status;
                    step.Result = completed.Result;
                    step.Error = completed.Error;
                }
                catch (OperationCanceledException)
                {
                    step.Status = AgentTaskStatus.Cancelled;
                    throw;
                }
                catch (Exception ex)
                {
                    step.Status = AgentTaskStatus.Failed;
                    step.Error = ex.Message;
                }
                finally
                {
                    step.CompletedAt = DateTimeOffset.Now;
                }

                if (step.Status == AgentTaskStatus.Failed)
                {
                    context.Progress?.Report(step.Error ?? "Step failed.");
                    break;
                }
            }
        }
        finally
        {
            context.Artifacts.ArtifactAdded -= OnArtifactAdded;
        }

        return new OrchestrationResult(plan, created, BuildSummary(plan));
    }

    private static string BuildSummary(Plan plan)
    {
        var lines = new List<string> { $"**Plan** - {plan.Summary}" };
        foreach (var step in plan.Steps)
        {
            var mark = step.Status switch
            {
                AgentTaskStatus.Succeeded => "done",
                AgentTaskStatus.Failed => "failed",
                AgentTaskStatus.Cancelled => "cancelled",
                _ => "skipped",
            };
            lines.Add($"- {step.Title} - {mark}{(step.Error is null ? string.Empty : $": {step.Error}")}");
        }
        return string.Join(Environment.NewLine, lines);
    }
}
