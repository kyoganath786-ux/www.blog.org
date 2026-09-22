using NickAI.Core.Models;
using NickAI.Core.Services;
using Xunit;

namespace NickAI.Core.Tests;

public sealed class PermissionServiceTests
{
    [Fact]
    public void Terminal_is_the_only_category_allowed_without_confirmation()
    {
        var service = new PermissionService();

        foreach (var category in Enum.GetValues<PermissionCategory>())
        {
            var expected = category == PermissionCategory.Terminal ? PermissionMode.Allow : PermissionMode.Ask;
            Assert.Equal(expected, service.ModeFor(category));
        }
    }

    [Fact]
    public async Task Allowed_category_runs_without_a_prompt()
    {
        var service = new PermissionService();

        var decision = await service.RequestAsync(PermissionCategory.Terminal, "dotnet build");

        Assert.True(decision.Allowed);
        Assert.False(decision.RequiresConfirmation);
    }

    [Fact]
    public async Task High_impact_category_requires_confirmation()
    {
        var service = new PermissionService();

        var decision = await service.RequestAsync(PermissionCategory.Publishing, "deploy to production");

        Assert.True(decision.Allowed);
        Assert.True(decision.RequiresConfirmation);
        Assert.Contains("Publishing", decision.Reason);
        Assert.Contains("deploy to production", decision.Reason);
    }

    [Fact]
    public async Task Denied_category_is_refused_with_a_reason()
    {
        var service = new PermissionService();
        service.SetMode(PermissionCategory.Camera, PermissionMode.Deny);

        var decision = await service.RequestAsync(PermissionCategory.Camera, "capture photo");

        Assert.False(decision.Allowed);
        Assert.False(decision.RequiresConfirmation);
        Assert.Contains("denied", decision.Reason);
    }

    [Fact]
    public async Task Remembering_a_choice_makes_it_automatic_in_future()
    {
        var service = new PermissionService();
        service.RememberChoice(PermissionCategory.Browser, allowedForever: true);

        Assert.Equal(PermissionMode.Allow, service.ModeFor(PermissionCategory.Browser));

        var decision = await service.RequestAsync(PermissionCategory.Browser, "open example.com");
        Assert.True(decision.Allowed);
        Assert.False(decision.RequiresConfirmation);
    }

    [Fact]
    public async Task Declining_to_remember_leaves_the_prompt_in_place()
    {
        var service = new PermissionService();
        service.RememberChoice(PermissionCategory.Files, allowedForever: false);

        Assert.Equal(PermissionMode.Ask, service.ModeFor(PermissionCategory.Files));

        var decision = await service.RequestAsync(PermissionCategory.Files, "write report.md");
        Assert.True(decision.RequiresConfirmation);
    }
}
