using NickAI.Core.Planning;
using Xunit;

namespace NickAI.Core.Tests;

/// <summary>
/// The classifier decides which agent handles a request, so these are the
/// highest-value tests in the suite: a wrong route means the user's
/// "Create a PowerPoint" never reaches the presentation agent.
/// </summary>
public sealed class RequestClassifierTests
{
    private readonly RequestClassifier _classifier = new();

    [Theory]
    // Spec section 2: the exact example requests NICK must understand.
    [InlineData("Create a game.", Capabilities.Game, "Building game")]
    [InlineData("Build a Windows app.", Capabilities.App, "Building application")]
    [InlineData("Create a website.", Capabilities.Web, "Building website")]
    [InlineData("Create a PowerPoint about AI.", Capabilities.Presentation, "Generating presentation")]
    [InlineData("Create a 15-slide presentation about artificial intelligence.", Capabilities.Presentation, "Generating presentation")]
    [InlineData("Create an Excel financial report.", Capabilities.Spreadsheet, "Generating spreadsheet")]
    [InlineData("Create an Excel sales dashboard.", Capabilities.Spreadsheet, "Generating spreadsheet")]
    [InlineData("Create a Word project report.", Capabilities.Document, "Generating document")]
    [InlineData("Generate an image.", Capabilities.Image, "Creating image")]
    [InlineData("Create a video.", Capabilities.Video, "Generating video")]
    [InlineData("Generate voice narration.", Capabilities.Audio, "Generating audio")]
    [InlineData("Fix my C# project.", Capabilities.Code, "Writing code")]
    [InlineData("Build my C# project as an EXE.", Capabilities.Build, "Building")]
    [InlineData("Build my application into an EXE.", Capabilities.Build, "Building")]
    [InlineData("Create a database schema for orders.", Capabilities.Database, "Designing database")]
    [InlineData("Analyze this CSV dataset.", Capabilities.Data, "Analyzing data")]
    [InlineData("Research the latest news on fusion power.", Capabilities.Research, "Researching")]
    [InlineData("Create a PDF invoice.", Capabilities.Pdf, "Generating PDF")]
    // Fallback: anything unrecognised is answered as a normal chat request.
    [InlineData("Hello, how are you?", Capabilities.Chat, "Answering")]
    [InlineData("Create a complete project from this idea.", Capabilities.Chat, "Answering")]
    public void Classify_routes_request_to_expected_capability(
        string request,
        string expectedCapability,
        string expectedDisplay)
    {
        var result = _classifier.Classify(request);

        Assert.Equal(expectedCapability, result.PrimaryCapability);
        Assert.Equal(expectedDisplay, result.DisplayName);
    }

    [Fact]
    public void Classify_is_case_insensitive()
    {
        Assert.Equal(Capabilities.Presentation, _classifier.Classify("MAKE ME A POWERPOINT").PrimaryCapability);
        Assert.Equal(Capabilities.Spreadsheet, _classifier.Classify("MAKE ME AN EXCEL SHEET").PrimaryCapability);
    }

    [Fact]
    public void Classify_does_not_match_keywords_inside_unrelated_words()
    {
        // "barcode" must not be treated as a code request.
        Assert.Equal(Capabilities.Chat, _classifier.Classify("scan this barcode").PrimaryCapability);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Classify_handles_empty_input(string? request)
    {
        var result = _classifier.Classify(request!);

        Assert.Equal(Capabilities.Chat, result.PrimaryCapability);
    }

    [Theory]
    // Capability -> the agent chain that actually implements it.
    [InlineData("Create a website.", Capabilities.Code)]
    [InlineData("Build a Windows app.", Capabilities.Code)]
    [InlineData("Create a database schema for orders.", Capabilities.Code)]
    [InlineData("Create a PowerPoint about AI.", Capabilities.Presentation)]
    [InlineData("Create a game.", Capabilities.Game)]
    [InlineData("Build my C# project as an EXE.", Capabilities.Build)]
    public void Classify_maps_capability_to_the_agent_chain_that_implements_it(string request, string expectedChainEntry)
    {
        var chain = _classifier.Classify(request).CapabilityChain;

        Assert.Single(chain);
        Assert.Equal(expectedChainEntry, chain[0]);
    }
}
