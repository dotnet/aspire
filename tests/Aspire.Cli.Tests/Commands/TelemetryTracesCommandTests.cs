// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class TelemetryTracesCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void TelemetryTracesCommand_Help_ShowsUsage()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var tracesCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "traces");
        Assert.NotNull(tracesCommand);

        // Verify command description
        Assert.False(string.IsNullOrEmpty(tracesCommand.Description));
    }

    [Fact]
    public async Task TelemetryTracesCommand_WithHelpArgument_ReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("telemetry traces --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void TelemetryTracesCommand_ExistsAsTelemetrySubcommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");

        Assert.NotNull(telemetryCommand);

        var tracesCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "traces");
        Assert.NotNull(tracesCommand);
        Assert.Equal("traces", tracesCommand.Name);
    }

    [Fact]
    public void TelemetryTracesCommand_HasResourceOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var tracesCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "traces");
        Assert.NotNull(tracesCommand);

        var resourceOption = tracesCommand.Options.FirstOrDefault(o => o.Name == "--resource");
        Assert.NotNull(resourceOption);
    }

    [Fact]
    public void TelemetryTracesCommand_HasFilterOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var tracesCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "traces");
        Assert.NotNull(tracesCommand);

        var filterOption = tracesCommand.Options.FirstOrDefault(o => o.Name == "--filter");
        Assert.NotNull(filterOption);
    }

    [Fact]
    public void TelemetryTracesCommand_HasSearchOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var tracesCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "traces");
        Assert.NotNull(tracesCommand);

        var searchOption = tracesCommand.Options.FirstOrDefault(o => o.Name == "--search");
        Assert.NotNull(searchOption);
    }

    [Fact]
    public void TelemetryTracesCommand_HasLimitOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var tracesCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "traces");
        Assert.NotNull(tracesCommand);

        var limitOption = tracesCommand.Options.FirstOrDefault(o => o.Name == "--limit");
        Assert.NotNull(limitOption);
    }

    [Fact]
    public void TelemetryTracesCommand_HasJsonOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var tracesCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "traces");
        Assert.NotNull(tracesCommand);

        var jsonOption = tracesCommand.Options.FirstOrDefault(o => o.Name == "--json");
        Assert.NotNull(jsonOption);
    }

    [Fact]
    public void TelemetryTracesCommand_HasTraceIdArgument()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var tracesCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "traces");
        Assert.NotNull(tracesCommand);

        var traceIdArg = tracesCommand.Arguments.FirstOrDefault(a => a.Name == "trace-id");
        Assert.NotNull(traceIdArg);
    }

    [Fact]
    public void TelemetryTracesCommand_InheritsParentOptions()
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
    public async Task TelemetryTracesCommand_NoDashboard_ReturnsError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();

        // Running without a dashboard connection should return an error
        var result = command.Parse("telemetry traces");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Expected to fail because no Dashboard is available
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void TelemetryTracesCommand_Help_ShowsAllOptions()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var tracesCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "traces");
        Assert.NotNull(tracesCommand);

        // Verify all expected options exist
        var optionNames = tracesCommand.Options.Select(o => o.Name).ToList();
        Assert.Contains("--resource", optionNames);
        Assert.Contains("--filter", optionNames);
        Assert.Contains("--search", optionNames);
        Assert.Contains("--limit", optionNames);
        Assert.Contains("--json", optionNames);
    }

    [Fact]
    public async Task TelemetryTracesCommand_InvalidFilterSyntax_ReturnsError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();

        // Use an invalid filter expression (missing operator)
        var result = command.Parse("telemetry traces --filter invalidfilter");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Expected to fail with InvalidArguments exit code (18) due to invalid filter syntax
        // The error should be caught before attempting Dashboard connection
        Assert.Equal(18, exitCode);
    }
}
