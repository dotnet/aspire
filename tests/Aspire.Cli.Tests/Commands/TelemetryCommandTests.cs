// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class TelemetryCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task TelemetryCommand_NoSubcommand_ShowsHelp()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("telemetry");

        // Running without subcommand should show help and return InvalidCommand exit code
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.InvalidCommand, exitCode);
    }

    [Fact]
    public async Task TelemetryCommand_WithHelpArgument_ReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("telemetry --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void TelemetryCommand_ExistsInRootCommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");

        Assert.NotNull(telemetryCommand);
        Assert.Equal("telemetry", telemetryCommand.Name);
    }

    [Fact]
    public void TelemetryCommand_Help_ShowsCommonOptions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");

        Assert.NotNull(telemetryCommand);

        // Verify common options are present (Name includes the -- prefix)
        var projectOption = telemetryCommand.Options.FirstOrDefault(o => o.Name == "--project");
        var dashboardUrlOption = telemetryCommand.Options.FirstOrDefault(o => o.Name == "--dashboard-url");
        var apiKeyOption = telemetryCommand.Options.FirstOrDefault(o => o.Name == "--api-key");

        Assert.NotNull(projectOption);
        Assert.NotNull(dashboardUrlOption);
        Assert.NotNull(apiKeyOption);
    }

    [Fact]
    public void TelemetryCommand_CommonOptions_AreRecursive()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");

        Assert.NotNull(telemetryCommand);

        // Verify options are marked as recursive (Name includes the -- prefix)
        var projectOption = telemetryCommand.Options.FirstOrDefault(o => o.Name == "--project");
        var dashboardUrlOption = telemetryCommand.Options.FirstOrDefault(o => o.Name == "--dashboard-url");
        var apiKeyOption = telemetryCommand.Options.FirstOrDefault(o => o.Name == "--api-key");

        Assert.NotNull(projectOption);
        Assert.NotNull(dashboardUrlOption);
        Assert.NotNull(apiKeyOption);

        Assert.True(projectOption.Recursive);
        Assert.True(dashboardUrlOption.Recursive);
        Assert.True(apiKeyOption.Recursive);
    }

    [Fact]
    public void TelemetryCommand_Description_IncludesExamples()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");

        Assert.NotNull(telemetryCommand);
        Assert.NotNull(telemetryCommand.Description);

        // Verify description includes examples section
        Assert.Contains("Examples:", telemetryCommand.Description);
        Assert.Contains("aspire telemetry traces", telemetryCommand.Description);
        Assert.Contains("aspire telemetry logs", telemetryCommand.Description);
        Assert.Contains("aspire telemetry metrics", telemetryCommand.Description);
        Assert.Contains("aspire telemetry fields", telemetryCommand.Description);
    }

    [Theory]
    [InlineData("traces", "aspire telemetry traces")]
    [InlineData("logs", "aspire telemetry logs")]
    [InlineData("metrics", "aspire telemetry metrics")]
    [InlineData("fields", "aspire telemetry fields")]
    public void TelemetrySubcommand_Description_IncludesExamples(string subcommandName, string expectedExample)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var subcommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == subcommandName);
        Assert.NotNull(subcommand);
        Assert.NotNull(subcommand.Description);

        // Verify description includes examples section with appropriate example
        Assert.Contains("Examples:", subcommand.Description);
        Assert.Contains(expectedExample, subcommand.Description);
    }

    [Fact]
    public void TelemetryTracesCommand_Description_IncludesFilterExamples()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var tracesCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "traces");
        Assert.NotNull(tracesCommand);
        Assert.NotNull(tracesCommand.Description);

        // Verify traces description includes filter usage examples
        Assert.Contains("--filter", tracesCommand.Description);
        Assert.Contains("--resource", tracesCommand.Description);
    }

    [Fact]
    public void TelemetryLogsCommand_Description_IncludesSeverityExample()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);
        Assert.NotNull(logsCommand.Description);

        // Verify logs description includes severity usage example
        Assert.Contains("--severity", logsCommand.Description);
    }

    [Fact]
    public void TelemetryMetricsCommand_Description_IncludesDurationExample()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var metricsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "metrics");
        Assert.NotNull(metricsCommand);
        Assert.NotNull(metricsCommand.Description);

        // Verify metrics description includes duration usage example
        Assert.Contains("--duration", metricsCommand.Description);
        Assert.Contains("--resource", metricsCommand.Description);
    }

    [Fact]
    public void TelemetryFieldsCommand_Description_IncludesTypeExample()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var fieldsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "fields");
        Assert.NotNull(fieldsCommand);
        Assert.NotNull(fieldsCommand.Description);

        // Verify fields description includes type usage example
        Assert.Contains("--type", fieldsCommand.Description);
    }
}
