// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using RootCommand = Aspire.Cli.Commands.RootCommand;

namespace Aspire.Cli.Tests.Commands;

public class TestCommandTests
{
    private readonly ITestOutputHelper _outputHelper;

    public TestCommandTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task TestCommandWithHelpArgumentReturnsZero()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper, options =>
        {
            options.EnabledFeatures = [KnownFeatures.TestCommandEnabled];
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var invokeConfiguration = new InvocationConfiguration();
        invokeConfiguration.Output = new TestOutputTextWriter(_outputHelper);

        var result = command.Parse("test --help");

        var exitCode = await result.InvokeAsync(invokeConfiguration).WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task TestCommand_WhenFeatureFlagEnabled_CommandAvailable()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper, options =>
        {
            options.EnabledFeatures = [KnownFeatures.TestCommandEnabled];
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var invokeConfiguration = new InvocationConfiguration();
        var testOutputWriter = new TestOutputTextWriter(_outputHelper);
        invokeConfiguration.Output = testOutputWriter;

        var result = command.Parse("test --help");

        var exitCode = await result.InvokeAsync(invokeConfiguration).WaitAsync(CliTestConstants.DefaultTimeout);

        // Should succeed because test command is registered when feature flag is enabled
        Assert.Equal(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task TestCommand_WhenFeatureFlagDisabled_CommandNotAvailable()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper, options =>
        {
            options.DisabledFeatures = [KnownFeatures.TestCommandEnabled];
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var invokeConfiguration = new InvocationConfiguration();
        var testOutputWriter = new TestOutputTextWriter(_outputHelper);
        invokeConfiguration.Output = testOutputWriter;

        var result = command.Parse("test");

        var exitCode = await result.InvokeAsync(invokeConfiguration).WaitAsync(CliTestConstants.DefaultTimeout);

        // Should fail because test command is not registered when feature flag is disabled
        Assert.NotEqual(ExitCodeConstants.Success, exitCode);
    }

    [Fact]
    public async Task TestCommand_WhenNoProjectFileFound_ReturnsFailedToFindProject()
    {
        using var workspace = TemporaryWorkspace.Create(_outputHelper);
        var services = CliTestHelper.CreateServiceCollection(workspace, _outputHelper, options =>
        {
            options.EnabledFeatures = [KnownFeatures.TestCommandEnabled];
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var invokeConfiguration = new InvocationConfiguration();
        var testOutputWriter = new TestOutputTextWriter(_outputHelper);
        invokeConfiguration.Output = testOutputWriter;

        var result = command.Parse("test");

        var exitCode = await result.InvokeAsync(invokeConfiguration).WaitAsync(CliTestConstants.DefaultTimeout);

        // Should fail because no project is found
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }
}
