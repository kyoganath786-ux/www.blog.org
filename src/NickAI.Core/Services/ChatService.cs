using System.Runtime.CompilerServices;
using System.Threading.Channels;
using NickAI.Core.Abstractions;
using NickAI.Core.Agents;
using NickAI.Core.Models;
using NickAI.Core.Planning;
using NickAI.Core.Providers;

namespace NickAI.Core.Services;

public enum ChatStreamEventKind
{
    Activity,
    Delta,
    Artifact,
    Plan,
    Error,
    Completed,
}

/// <summary>A single event emitted while handling a user message.</summary>
public sealed record ChatStreamEvent(
    ChatStreamEventKind Kind,
    string? Text = null,
    Artifact? Artifact = null,
    Plan? Plan = null);

/// <summary>
/// Decides how to handle a message: plain chat streams directly from the model,
/// while task-shaped requests run through the orchestrator. Capabilities with no
/// registered agent are reported as unsupported rather than faked.
/// </summary>
public sealed class ChatService
{
    private readonly AgentRegistry _registry;
    private readonly OrchestratorAgent _orchestrator;
    private readonly RequestClassifier _classifier;
    private readonly ModelManager _models;
    private readonly ModelRouter _router;
    private readonly IArtifactStore _artifacts;

    public ChatService(
        AgentRegistry registry,
        OrchestratorAgent orchestrator,
        RequestClassifier classifier,
        ModelManager models,
        ModelRouter router,
        IArtifactStore artifacts)
    {
        _registry = registry;
        _orchestrator = orchestrator;
        _classifier = classifier;
        _models = models;
        _router = router;
        _artifacts = artifacts;
    }

    public async IAsyncEnumerable<ChatStreamEvent> SendAsync(
        Conversation conversation,
        string userText,
        Project? project,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var classification = _classifier.Classify(userText);
        var model = _router.Route(classification.PrimaryCapability) ?? _models.ActiveModel;

        if (string.IsNullOrWhiteSpace(model))
        {
            yield return new ChatStreamEvent(
                ChatStreamEventKind.Error,
                "No local model is available. Start Ollama, pull a model, then refresh the model list.");
            yield break;
        }

        var provider = _models.ProviderFor(model);
        if (provider is null)
        {
            yield return new ChatStreamEvent(ChatStreamEventKind.Error, $"No provider can serve model '{model}'.");
            yield break;
        }

        var chain = classification.CapabilityChain;
        var isPlainChat = chain.Count == 1 && chain[0] == Capabilities.Chat;

        if (!isPlainChat && !chain.Any(_registry.Has))
        {
            var implemented = string.Join(", ", _registry.ImplementedCapabilities);
            yield return new ChatStreamEvent(
                ChatStreamEventKind.Error,
                $"{classification.DisplayName} is not implemented in this build yet. Implemented capabilities: {implemented}.");
            yield break;
        }

        if (isPlainChat)
        {
            var messages = conversation.Messages
                .Where(m => m.Role is ChatRole.User or ChatRole.Assistant)
                .Select(m => new ChatMessage { Role = m.Role, Content = m.Content })
                .ToList();

            var request = new ModelRequest
            {
                Model = model,
                Messages = messages,
                SystemPrompt = Prompts.Assistant,
            };

            await using var enumerator = provider.StreamChatAsync(request, ct).GetAsyncEnumerator(ct);
            while (true)
            {
                string current = string.Empty;
                bool hasNext;
                string? streamError = null;

                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                    current = enumerator.Current;
                }
                catch (Exception ex)
                {
                    streamError = ex.Message;
                    hasNext = false;
                }

                if (streamError is not null)
                {
                    yield return new ChatStreamEvent(ChatStreamEventKind.Error, streamError);
                    yield break;
                }

                if (!hasNext) break;

                yield return new ChatStreamEvent(ChatStreamEventKind.Delta, current);
            }

            yield return new ChatStreamEvent(ChatStreamEventKind.Completed);
            yield break;
        }

        // Task-shaped request: run the orchestrator and surface live activity.
        var progressChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        var context = new AgentContext
        {
            Request = userText,
            Conversation = conversation,
            Project = project,
            Models = provider,
            Model = model,
            Artifacts = _artifacts,
            Progress = new Progress<string>(message => progressChannel.Writer.TryWrite(message)),
        };

        var runTask = _orchestrator.RunAsync(userText, context, ct);
        _ = runTask.ContinueWith(_ => progressChannel.Writer.TryComplete(), TaskScheduler.Default);

        await foreach (var activity in progressChannel.Reader.ReadAllAsync(ct))
        {
            yield return new ChatStreamEvent(ChatStreamEventKind.Activity, activity);
        }

        OrchestrationResult? outcome = null;
        string? runError = null;
        try
        {
            outcome = await runTask;
        }
        catch (Exception ex)
        {
            runError = ex.Message;
        }

        if (runError is not null)
        {
            yield return new ChatStreamEvent(ChatStreamEventKind.Error, runError);
            yield break;
        }

        if (outcome is not null)
        {
            yield return new ChatStreamEvent(ChatStreamEventKind.Plan, Plan: outcome.Plan);
            foreach (var artifact in outcome.Artifacts)
                yield return new ChatStreamEvent(ChatStreamEventKind.Artifact, Artifact: artifact);
            yield return new ChatStreamEvent(ChatStreamEventKind.Delta, outcome.Summary);
        }

        yield return new ChatStreamEvent(ChatStreamEventKind.Completed);
    }
}
