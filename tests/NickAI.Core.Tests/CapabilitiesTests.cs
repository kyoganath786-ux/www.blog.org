using System.Reflection;
using NickAI.Core.Models;
using NickAI.Core.Planning;
using Xunit;

namespace NickAI.Core.Tests;

public sealed class CapabilitiesTests
{
    [Theory]
    [InlineData(Capabilities.Chat, "Answering")]
    [InlineData(Capabilities.Code, "Writing code")]
    [InlineData(Capabilities.Presentation, "Generating presentation")]
    [InlineData(Capabilities.Spreadsheet, "Generating spreadsheet")]
    [InlineData(Capabilities.Document, "Generating document")]
    [InlineData(Capabilities.Pdf, "Generating PDF")]
    [InlineData(Capabilities.Image, "Creating image")]
    [InlineData(Capabilities.Video, "Generating video")]
    [InlineData(Capabilities.Audio, "Generating audio")]
    [InlineData(Capabilities.Web, "Building website")]
    [InlineData(Capabilities.App, "Building application")]
    [InlineData(Capabilities.Game, "Building game")]
    [InlineData(Capabilities.Database, "Designing database")]
    [InlineData(Capabilities.Data, "Analyzing data")]
    [InlineData(Capabilities.Research, "Researching")]
    [InlineData(Capabilities.Build, "Building")]
    [InlineData(Capabilities.Design, "Designing")]
    public void Display_uses_human_readable_activity_text(string capability, string expected)
    {
        Assert.Equal(expected, Capabilities.Display(capability));
    }

    [Fact]
    public void Display_is_case_insensitive_and_falls_back_to_the_key()
    {
        Assert.Equal("Writing code", Capabilities.Display("CODE"));
        Assert.Equal("SomethingNew", Capabilities.Display("SomethingNew"));
    }

    [Fact]
    public void Every_declared_capability_has_a_display_name()
    {
        var keys = typeof(Capabilities)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false })
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(keys);
        foreach (var key in keys)
        {
            // A missing display name leaks the raw key into the activity log.
            Assert.NotEqual(key, Capabilities.Display(key));
        }
    }

    [Fact]
    public void Model_capability_flags_are_independent()
    {
        var capabilities = ModelCapabilities.Completion | ModelCapabilities.Vision;

        var model = new ModelInfo { Name = "llava:7b", Capabilities = capabilities };

        Assert.True(model.Supports(ModelCapabilities.Vision));
        Assert.True(model.Supports(ModelCapabilities.Completion));
        Assert.False(model.Supports(ModelCapabilities.Embedding));
        Assert.False(model.Supports(ModelCapabilities.Tools));
        Assert.False(model.Supports(ModelCapabilities.Speech));
    }

    [Fact]
    public void Models_default_to_completion_only()
    {
        var model = new ModelInfo { Name = "llama3.2:3b" };

        Assert.True(model.Supports(ModelCapabilities.Completion));
        Assert.False(model.Supports(ModelCapabilities.Vision));
        Assert.Equal("unknown", model.SizeDisplay);
    }
}
