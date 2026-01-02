// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class TelemetryLogsCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void TelemetryLogsCommand_Help_ShowsUsage()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);

        // Verify command description
        Assert.False(string.IsNullOrEmpty(logsCommand.Description));
    }

    [Fact]
    public async Task TelemetryLogsCommand_WithHelpArgument_ReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("telemetry logs --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void TelemetryLogsCommand_ExistsAsTelemetrySubcommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");

        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);
        Assert.Equal("logs", logsCommand.Name);
    }

    [Fact]
    public void TelemetryLogsCommand_HasResourceOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);

        var resourceOption = logsCommand.Options.FirstOrDefault(o => o.Name == "--resource");
        Assert.NotNull(resourceOption);
    }

    [Fact]
    public void TelemetryLogsCommand_HasTraceOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);

        var traceOption = logsCommand.Options.FirstOrDefault(o => o.Name == "--trace");
        Assert.NotNull(traceOption);
    }

    [Fact]
    public void TelemetryLogsCommand_HasSpanOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);

        var spanOption = logsCommand.Options.FirstOrDefault(o => o.Name == "--span");
        Assert.NotNull(spanOption);
    }

    [Fact]
    public void TelemetryLogsCommand_HasFilterOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);

        var filterOption = logsCommand.Options.FirstOrDefault(o => o.Name == "--filter");
        Assert.NotNull(filterOption);
    }

    [Fact]
    public void TelemetryLogsCommand_HasSeverityOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);

        var severityOption = logsCommand.Options.FirstOrDefault(o => o.Name == "--severity");
        Assert.NotNull(severityOption);
    }

    [Fact]
    public void TelemetryLogsCommand_HasLimitOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);

        var limitOption = logsCommand.Options.FirstOrDefault(o => o.Name == "--limit");
        Assert.NotNull(limitOption);
    }

    [Fact]
    public void TelemetryLogsCommand_HasJsonOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);

        var jsonOption = logsCommand.Options.FirstOrDefault(o => o.Name == "--json");
        Assert.NotNull(jsonOption);
    }

    [Fact]
    public void TelemetryLogsCommand_InheritsParentOptions()
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
    public async Task TelemetryLogsCommand_NoDashboard_ReturnsError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();

        // Running without a dashboard connection should return an error
        var result = command.Parse("telemetry logs");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Expected to fail because no Dashboard is available
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void TelemetryLogsCommand_Help_ShowsAllOptions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);

        // Verify all expected options exist
        var optionNames = logsCommand.Options.Select(o => o.Name).ToList();
        Assert.Contains("--resource", optionNames);
        Assert.Contains("--trace", optionNames);
        Assert.Contains("--span", optionNames);
        Assert.Contains("--filter", optionNames);
        Assert.Contains("--severity", optionNames);
        Assert.Contains("--limit", optionNames);
        Assert.Contains("--json", optionNames);
    }

    [Fact]
    public async Task TelemetryLogsCommand_InvalidFilterSyntax_ReturnsError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();

        // Use an invalid filter expression (missing operator)
        var result = command.Parse("telemetry logs --filter invalidfilter");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Expected to fail with InvalidArguments exit code (18) due to invalid filter syntax
        // The error should be caught before attempting Dashboard connection
        Assert.Equal(18, exitCode);
    }

    [Fact]
    public void TelemetryLogsCommand_LimitOption_HasDefaultValue()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var logsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "logs");
        Assert.NotNull(logsCommand);

        var limitOption = logsCommand.Options.FirstOrDefault(o => o.Name == "--limit");
        Assert.NotNull(limitOption);

        // Verify the option has a default value factory
        Assert.True(limitOption.HasDefaultValue, "--limit option should have a default value");
    }

    [Fact]
    public async Task TelemetryLogsCommand_InvalidSeverity_ReturnsError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();

        // Use an invalid severity value
        var result = command.Parse("telemetry logs --severity NotAValidSeverity");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Expected to fail with InvalidCommand exit code (1) due to invalid severity
        // The error should be caught before attempting Dashboard connection
        Assert.Equal(1, exitCode);
    }
}
