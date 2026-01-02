// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Tests.Commands;

public class TelemetryFieldsCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void TelemetryFieldsCommand_Help_ShowsUsage()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var fieldsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "fields");
        Assert.NotNull(fieldsCommand);

        // Verify command description
        Assert.False(string.IsNullOrEmpty(fieldsCommand.Description));
    }

    [Fact]
    public async Task TelemetryFieldsCommand_WithHelpArgument_ReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("telemetry fields --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void TelemetryFieldsCommand_ExistsAsTelemetrySubcommand()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");

        Assert.NotNull(telemetryCommand);

        var fieldsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "fields");
        Assert.NotNull(fieldsCommand);
        Assert.Equal("fields", fieldsCommand.Name);
    }

    [Fact]
    public void TelemetryFieldsCommand_HasTypeOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var fieldsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "fields");
        Assert.NotNull(fieldsCommand);

        var typeOption = fieldsCommand.Options.FirstOrDefault(o => o.Name == "--type");
        Assert.NotNull(typeOption);
    }

    [Fact]
    public void TelemetryFieldsCommand_HasResourceOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var fieldsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "fields");
        Assert.NotNull(fieldsCommand);

        var resourceOption = fieldsCommand.Options.FirstOrDefault(o => o.Name == "--resource");
        Assert.NotNull(resourceOption);
    }

    [Fact]
    public void TelemetryFieldsCommand_HasJsonOption()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var fieldsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "fields");
        Assert.NotNull(fieldsCommand);

        var jsonOption = fieldsCommand.Options.FirstOrDefault(o => o.Name == "--json");
        Assert.NotNull(jsonOption);
    }

    [Fact]
    public void TelemetryFieldsCommand_HasFieldNameArgument()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var rootCommand = provider.GetRequiredService<RootCommand>();
        var telemetryCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "telemetry");
        Assert.NotNull(telemetryCommand);

        var fieldsCommand = telemetryCommand.Subcommands.FirstOrDefault(c => c.Name == "fields");
        Assert.NotNull(fieldsCommand);

        var fieldNameArg = fieldsCommand.Arguments.FirstOrDefault(a => a.Name == "field-name");
        Assert.NotNull(fieldNameArg);
    }

    [Fact]
    public void TelemetryFieldsCommand_InheritsParentOptions()
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
    public async Task TelemetryFieldsCommand_NoDashboard_ReturnsError()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();

        // Running without a dashboard connection should return an error
        var result = command.Parse("telemetry fields");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Expected to fail because no Dashboard is available
        Assert.NotEqual(0, exitCode);
    }
}
