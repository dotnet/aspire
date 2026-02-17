// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Tests.Utils;

public class ConsoleActivityLoggerTests
{
    [Fact]
    public void WriteSummary_WithMarkdownLinkInPipelineSummary_RendersClickableLink()
    {
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });

        var hostEnvironment = TestHelpers.CreateInteractiveHostEnvironment();
        var logger = new ConsoleActivityLogger(console, hostEnvironment, forceColor: true);

        var summary = new List<KeyValuePair<string, string>>
        {
            new("☁️ Target", "Azure"),
            new("📦 Resource Group", "VNetTest5 [link](https://portal.azure.com/#/resource/subscriptions/sub-id/resourceGroups/VNetTest5/overview)"),
            new("🔑 Subscription", "sub-id"),
            new("🌐 Location", "eastus"),
        };

        logger.SetFinalResult(true, summary);
        logger.WriteSummary();

        var result = output.ToString();

        // Verify the markdown link was converted to a Spectre link
        Assert.Contains("VNetTest5", result);

        const string expectedUrl =
            @"https://portal\.azure\.com/#/resource/subscriptions/sub-id/resourceGroups/VNetTest5/overview";
        string hyperlinkPattern =
            $@"\u001b\]8;[^;]*;{expectedUrl}\u001b\\.*link.*\u001b\]8;;\u001b\\";
        Assert.Matches(hyperlinkPattern, result);
    }

    [Fact]
    public void WriteSummary_WithMarkdownLinkInPipelineSummary_NoColor_RendersPlainTextWithUrl()
    {
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });

        var hostEnvironment = TestHelpers.CreateNonInteractiveHostEnvironment();
        var logger = new ConsoleActivityLogger(console, hostEnvironment, forceColor: false);

        var portalUrl = "https://portal.azure.com/";
        var summary = new List<KeyValuePair<string, string>>
        {
            new("📦 Resource Group", $"VNetTest5 [link]({portalUrl})"),
        };

        logger.SetFinalResult(true, summary);
        logger.WriteSummary();

        var result = output.ToString();

        // When color is disabled, markdown links should be converted to plain text: text (url)
        Assert.Contains($"VNetTest5 link ({portalUrl})", result);
    }

    [Fact]
    public void WriteSummary_WithPlainTextPipelineSummary_RendersCorrectly()
    {
        var output = new StringBuilder();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(new StringWriter(output))
        });

        var hostEnvironment = TestHelpers.CreateInteractiveHostEnvironment();
        var logger = new ConsoleActivityLogger(console, hostEnvironment, forceColor: true);

        var summary = new List<KeyValuePair<string, string>>
        {
            new("☁️ Target", "Azure"),
            new("🌐 Location", "eastus"),
        };

        logger.SetFinalResult(true, summary);
        logger.WriteSummary();

        var result = output.ToString();

        Assert.Contains("Azure", result);
        Assert.Contains("eastus", result);
    }
}
