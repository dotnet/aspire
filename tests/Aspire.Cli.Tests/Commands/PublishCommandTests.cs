// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Tests.TestServices;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class PublishCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task PublishCommandWithHelpArgumentReturnsZero()
    {
        var services = CliTestHelper.CreateServiceCollection(outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("publish --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task PublishCommandFailsWithInvalidProjectFile()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.GetAppHostInformationAsyncCallback = (projectFile, cancellationToken) =>
                {
                    return (1, false, null); // Simulate failure to retrieve app host information
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("publish --project invalid.csproj");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.NotEqual(0, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task PublishCommandFailsWhenDotNetCliRunnerThrows()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.BuildAsyncCallback = (projectFile, cancellationToken) =>
                {
                    throw new InvalidOperationException("Simulated failure in DotNetCliRunner");
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("publish --project valid.csproj");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.NotEqual(0, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task PublishCommandFailsWhenAppHostIsNotCompatible()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.GetAppHostInformationAsyncCallback = (projectFile, cancellationToken) =>
                {
                    return (0, false, "9.0.0"); // Simulate an incompatible app host
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("publish --project valid.csproj");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.NotEqual(0, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task PublishCommandFailsWhenAppHostBuildFails()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.BuildAsyncCallback = (projectFile, cancellationToken) =>
                {
                    return 1; // Simulate a build failure
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("publish --project valid.csproj");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.NotEqual(0, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task PublishCommandFailsWhenAppHostCrashesBeforeBackchannelEstablished()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();

                // Simulate a successful build
                runner.BuildAsyncCallback = (projectFile, cancellationToken) => 0;

                // Simulate apphost starting but crashing before backchannel is established
                runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, cancellationToken) =>
                {
                    // Simulate a delay to mimic apphost starting
                    await Task.Delay(100, cancellationToken);

                    // Simulate apphost crash by completing the backchannel with an exception
                    backchannelCompletionSource?.SetException(new InvalidOperationException("AppHost process has exited unexpectedly. Use --debug to see more deails."));

                    return 1; // Non-zero exit code to indicate failure
                };

                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("publish --project valid.csproj");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.NotEqual(0, exitCode); // Ensure the command fails
    }
}
