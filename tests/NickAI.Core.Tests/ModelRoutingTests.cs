using NickAI.Core.Models;
using NickAI.Core.Providers;
using NickAI.Core.Tests.Fakes;
using Xunit;

namespace NickAI.Core.Tests;

public sealed class ModelRoutingTests
{
    private static readonly ModelInfo Llama = new()
    {
        Name = "llama3.2:3b",
        Provider = "test",
        Capabilities = ModelCapabilities.Completion,
    };

    private static readonly ModelInfo Coder = new()
    {
        Name = "qwen2.5-coder:7b",
        Provider = "test",
        Capabilities = ModelCapabilities.Completion,
    };

    private static readonly ModelInfo Embedder = new()
    {
        Name = "nomic-embed-text",
        Provider = "test",
        Capabilities = ModelCapabilities.Embedding,
    };

    private static readonly ModelInfo Vision = new()
    {
        Name = "llava:7b",
        Provider = "test",
        Capabilities = ModelCapabilities.Vision | ModelCapabilities.Completion,
    };

    private static readonly ModelInfo ToolUser = new()
    {
        Name = "qwen3:8b",
        Provider = "test",
        Capabilities = ModelCapabilities.Completion | ModelCapabilities.Tools,
    };

    private static async Task<ModelManager> ManagerAsync(params ModelInfo[] models)
    {
        var manager = new ModelManager();
        manager.Register(new FakeModelProvider("test", available: true, models));
        await manager.RefreshAsync();
        return manager;
    }

    [Fact]
    public async Task RefreshAsync_loads_models_and_activates_the_first_one()
    {
        var manager = await ManagerAsync(Llama, Coder, Embedder, Vision, ToolUser);

        Assert.Equal(5, manager.Models.Count);
        Assert.Equal("llama3.2:3b", manager.ActiveModel);
    }

    [Fact]
    public async Task RefreshAsync_ignores_providers_that_are_not_running()
    {
        var manager = new ModelManager();
        manager.Register(new FakeModelProvider("ollama", available: false, Llama));

        await manager.RefreshAsync();

        Assert.Empty(manager.Models);
        Assert.Null(manager.ActiveModel);
    }

    [Fact]
    public async Task RefreshAsync_survives_a_provider_that_throws()
    {
        var manager = new ModelManager();
        manager.Register(new FakeModelProvider("flaky", available: true, Llama) { ThrowOnList = true });

        await manager.RefreshAsync(); // must not throw

        Assert.Empty(manager.Models);
    }

    [Fact]
    public async Task RefreshAsync_notifies_listeners()
    {
        var manager = new ModelManager();
        manager.Register(new FakeModelProvider("test", available: true, Llama));
        var notifications = 0;
        manager.Changed += (_, _) => notifications++;

        await manager.RefreshAsync();

        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task SelectModel_changes_the_active_model_and_notifies()
    {
        var manager = await ManagerAsync(Llama, Coder);
        var notifications = 0;
        manager.Changed += (_, _) => notifications++;

        manager.SelectModel("qwen2.5-coder:7b");

        Assert.Equal("qwen2.5-coder:7b", manager.ActiveModel);
        Assert.Equal(1, notifications);

        manager.SelectModel("   ");
        Assert.Equal("qwen2.5-coder:7b", manager.ActiveModel);
    }

    [Fact]
    public async Task RefreshAsync_keeps_the_users_model_selection_when_it_is_still_installed()
    {
        var manager = await ManagerAsync(Llama, Coder);
        manager.SelectModel("qwen2.5-coder:7b");

        await manager.RefreshAsync();

        Assert.Equal("qwen2.5-coder:7b", manager.ActiveModel);
    }

    [Theory]
    [InlineData("code")]
    [InlineData("debug")]
    [InlineData("build")]
    public async Task Route_prefers_a_coder_model_for_development_work(string capability)
    {
        var manager = await ManagerAsync(Llama, Coder, Embedder, Vision, ToolUser);

        Assert.Equal("qwen2.5-coder:7b", new ModelRouter(manager).Route(capability));
    }

    [Theory]
    [InlineData("embedding")]
    [InlineData("rag")]
    [InlineData("knowledge")]
    public async Task Route_picks_an_embedding_model_for_knowledge_work(string capability)
    {
        var manager = await ManagerAsync(Llama, Coder, Embedder, Vision, ToolUser);

        Assert.Equal("nomic-embed-text", new ModelRouter(manager).Route(capability));
    }

    [Fact]
    public async Task Route_picks_a_vision_model_only_when_one_is_installed()
    {
        var router = new ModelRouter(await ManagerAsync(Llama, Coder, Embedder, Vision));
        Assert.Equal("llava:7b", router.Route("vision"));

        var withoutVision = new ModelRouter(await ManagerAsync(Llama, Coder));
        // No vision model installed: fall back to the active model rather than
        // pretending a text-only model can see.
        Assert.Equal("llama3.2:3b", withoutVision.Route("vision"));
    }

    [Theory]
    [InlineData("tools")]
    [InlineData("browser")]
    [InlineData("terminal")]
    public async Task Route_picks_a_tool_capable_model_for_agent_work(string capability)
    {
        var manager = await ManagerAsync(Llama, Coder, Embedder, Vision, ToolUser);

        Assert.Equal("qwen3:8b", new ModelRouter(manager).Route(capability));
    }

    [Fact]
    public async Task Route_uses_the_active_model_for_plain_chat()
    {
        var manager = await ManagerAsync(Llama, Coder);

        Assert.Equal("llama3.2:3b", new ModelRouter(manager).Route("chat"));
    }

    [Fact]
    public async Task Route_returns_null_when_no_provider_has_any_model()
    {
        var manager = new ModelManager();
        manager.Register(new FakeModelProvider("ollama", available: false));

        await manager.RefreshAsync();

        Assert.Null(new ModelRouter(manager).Route("code"));
    }
}
