using NickAI.Core.Abstractions;
using NickAI.Core.Agents;
using NickAI.Core.Models;
using NickAI.Core.Planning;
using NickAI.Core.Tests.Fakes;
using Xunit;

namespace NickAI.Core.Tests;

public sealed class OrchestratorAgentTests
{
    private static readonly RequestClassifier Classifier = new();

    [Fact]
    public void Plan_turns_a_request_into_ordered_steps()
    {
        var orchestrator = new OrchestratorAgent(new AgentRegistry(), Classifier);

        var plan = orchestrator.Plan("Create a website.");

        var step = Assert.Single(plan.Steps);
        // The step names the agent that will actually run; the plan summary
        // carries the user-facing wording.
        Assert.Equal("1. Writing code", step.Title);
        Assert.Equal(Capabilities.Code, step.Capability);
        Assert.Equal("Create a website.", step.Description);
        Assert.Equal(AgentTaskStatus.Pending, step.Status);
        Assert.Equal("Create a website.", plan.Goal);
        Assert.Equal("Building website", plan.Summary);
    }

    [Fact]
    public async Task RunAsync_dispatches_to_the_agent_and_collects_its_artifacts()
    {
        var store = new FakeArtifactStore();
        var registry = new AgentRegistry();
        var agent = new FakeAgent(Capabilities.Code, artifactName: "index.html");
        registry.Register(agent);

        var result = await new OrchestratorAgent(registry, Classifier)
            .RunAsync("Create a website.", Context(store, "Create a website."));

        Assert.Equal(1, agent.Executions);
        Assert.Equal(AgentTaskStatus.Succeeded, result.Plan.Steps[0].Status);
        Assert.Equal("ok", result.Plan.Steps[0].Result);

        var artifact = Assert.Single(result.Artifacts);
        Assert.Equal("index.html", artifact.Name);
        Assert.Contains("done", result.Summary);
    }

    [Fact]
    public async Task RunAsync_reports_unimplemented_capabilities_instead_of_faking_output()
    {
        var store = new FakeArtifactStore();
        var progress = new List<string>();
        var registry = new AgentRegistry();
        registry.Register(new FakeAgent(Capabilities.Chat));

        var result = await new OrchestratorAgent(registry, Classifier)
            .RunAsync("Create a PowerPoint about AI.", Context(store, "Create a PowerPoint about AI.", progress));

        var step = Assert.Single(result.Plan.Steps);
        Assert.Equal(AgentTaskStatus.Failed, step.Status);
        Assert.NotNull(step.Error);
        Assert.Contains("not implemented", step.Error);
        Assert.Empty(result.Artifacts);
        Assert.Empty(store.All);
        Assert.Contains(progress, line => line.Contains("not implemented"));
    }

    [Fact]
    public async Task RunAsync_turns_an_agent_exception_into_a_failed_step()
    {
        var store = new FakeArtifactStore();
        var registry = new AgentRegistry();
        registry.Register(new FakeAgent(Capabilities.Code, failWith: new InvalidOperationException("compile failed")));

        var result = await new OrchestratorAgent(registry, Classifier)
            .RunAsync("Create a website.", Context(store, "Create a website."));

        var step = Assert.Single(result.Plan.Steps);
        Assert.Equal(AgentTaskStatus.Failed, step.Status);
        Assert.Equal("compile failed", step.Error);
        Assert.Contains("failed", result.Summary);
    }

    [Fact]
    public void Registry_only_claims_capabilities_that_have_a_real_agent()
    {
        var registry = new AgentRegistry();
        registry.Register(new FakeAgent(Capabilities.Code));

        Assert.True(registry.Has(Capabilities.Code));
        Assert.False(registry.Has(Capabilities.Presentation));
        Assert.Equal(new[] { Capabilities.Code }, registry.ImplementedCapabilities);
        Assert.False(registry.TryGet(Capabilities.Presentation, out _));
    }

    private static AgentContext Context(FakeArtifactStore store, string request, List<string>? progress = null) => new()
    {
        Request = request,
        Conversation = new Conversation(),
        Models = new FakeModelProvider("test", available: true),
        Model = "llama3.2:3b",
        Artifacts = store,
        Progress = new SyncProgress(progress ?? new List<string>()),
    };

    /// <summary>IProgress that records synchronously so assertions are deterministic.</summary>
    private sealed class SyncProgress : IProgress<string>
    {
        private readonly List<string> _lines;

        public SyncProgress(List<string> lines) => _lines = lines;

        public void Report(string value) => _lines.Add(value);
    }
}
