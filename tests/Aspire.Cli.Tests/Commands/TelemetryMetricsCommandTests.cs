// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class TelemetryMetricsCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void TelemetryMetricsCommand_Help_ShowsUsage()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var metricsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "metrics");
        Assert.NotNull(metricsCommand);

        // Verify command description
        Assert.False(string.IsNullOrEmpty(metricsCommand.Description));
    }

    [Fact]
    public async Task TelemetryMetricsCommand_WithHelpArgument_ReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("telemetry metrics --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void TelemetryMetricsCommand_ExistsAsTelemetrySubcommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");

        Assert.NotNull(telemetryCommand);

        var metricsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "metrics");
        Assert.NotNull(metricsCommand);
        Assert.Equal("metrics", metricsCommand.Name);
    }

    [Fact]
    public void TelemetryMetricsCommand_HasResourceOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var metricsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "metrics");
        Assert.NotNull(metricsCommand);

        var resourceOption = metricsCommand.Options.FirstOrDefault(o => o.Name == "--resource");
        Assert.NotNull(resourceOption);
    }

    [Fact]
    public void TelemetryMetricsCommand_HasDurationOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var metricsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "metrics");
        Assert.NotNull(metricsCommand);

        var durationOption = metricsCommand.Options.FirstOrDefault(o => o.Name == "--duration");
        Assert.NotNull(durationOption);
    }

    [Fact]
    public void TelemetryMetricsCommand_HasJsonOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var metricsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "metrics");
        Assert.NotNull(metricsCommand);

        var jsonOption = metricsCommand.Options.FirstOrDefault(o => o.Name == "--json");
        Assert.NotNull(jsonOption);
    }

    [Fact]
    public void TelemetryMetricsCommand_HasInstrumentArgument()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var metricsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "metrics");
        Assert.NotNull(metricsCommand);

        var instrumentArg = metricsCommand.Arguments.FirstOrDefault(a => a.Name == "meter/instrument");
        Assert.NotNull(instrumentArg);
    }

    [Fact]
    public void TelemetryMetricsCommand_InheritsParentOptions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        // Parent options (--project, --dashboard-url, --api-key) should be accessible from subcommands
        // due to Recursive = true setting on the parent
        var projectOption = telemetryCommand.Options.FirstOrDefault(o => o.Name == "--project");
        var dashboardUrlOption = telemetryCommand.Options.FirstOrDefault(o => o.Name == "--dashboard-url");
        var apiKeyOption = telemetryCommand.Options.FirstOrDefault(o => o.Name == "--api-key");

        Assert.NotNull(projectOption);
        Assert.NotNull(dashboardUrlOption);
        Assert.NotNull(apiKeyOption);

        Assert.True(projectOption.Recursive, "--project option should be recursive");
        Assert.True(dashboardUrlOption.Recursive, "--dashboard-url option should be recursive");
        Assert.True(apiKeyOption.Recursive, "--api-key option should be recursive");
    }

    [Fact]
    public async Task TelemetryMetricsCommand_NoDashboard_ReturnsError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();

        // Running without a dashboard connection but with required --resource should return an error
        var result = command.Parse("telemetry metrics --resource test-resource");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Expected to fail because no Dashboard is available
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void TelemetryMetricsCommand_Help_ShowsAllOptions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var metricsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "metrics");
        Assert.NotNull(metricsCommand);

        // Verify all expected options exist
        var optionNames = metricsCommand.Options.Select(o => o.Name).ToList();
        Assert.Contains("--resource", optionNames);
        Assert.Contains("--duration", optionNames);
        Assert.Contains("--json", optionNames);
    }
}
