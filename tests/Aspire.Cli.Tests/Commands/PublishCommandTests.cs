// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Tests.TestServices;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Aspire.Cli.Utils;

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
                runner.GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
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
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task PublishCommandFailsWhenAppHostIsNotCompatible()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
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
        Assert.Equal(ExitCodeConstants.AppHostIncompatible, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task PublishCommandFailsWhenAppHostBuildFails()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();
                runner.BuildAsyncCallback = (projectFile, options, cancellationToken) =>
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
        Assert.Equal(ExitCodeConstants.FailedToBuildArtifacts, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task PublishCommandFailsWhenAppHostCrashesBeforeBackchannelEstablished()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();

                // Simulate a successful build
                runner.BuildAsyncCallback = (projectFile, options, cancellationToken) => 0;

                // Simulate apphost starting but crashing before backchannel is established
                runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                {
                    // Simulate a delay to mimic apphost starting
                    await Task.Delay(100, cancellationToken);

                    // Simulate apphost crash by completing the backchannel with an exception
                    backchannelCompletionSource?.SetException(new InvalidOperationException("AppHost process has exited unexpectedly. Use --debug to see more details."));

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
        Assert.Equal(ExitCodeConstants.FailedToBuildArtifacts, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task PublishCommandSucceedsEndToEnd()
    {
        // Arrange
        var services = CliTestHelper.CreateServiceCollection(outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner();

                // Simulate a successful build
                runner.BuildAsyncCallback = (projectFile, options, cancellationToken) => 0;
                
                // Simulate a successful app host information retrieval
                runner.GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                {
                    return (0, true, VersionHelper.GetDefaultTemplateVersion()); // Compatible app host with backchannel support
                };

                // Simulate apphost running successfully and establishing a backchannel
                runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                {
                    Assert.True(options.NoLaunchProfile);

                    if (args.Any(a => a == "inspect"))
                    {
                        var inspectModeCompleted = new TaskCompletionSource();
                        var backchannel = new TestAppHostBackchannel();
                        backchannel.RequestStopAsyncCalled = inspectModeCompleted;
                        backchannelCompletionSource?.SetResult(backchannel);
                        await inspectModeCompleted.Task;
                        return 0;
                    }
                    else
                    {
                        var publishModeCompleted = new TaskCompletionSource();
                        var backchannel = new TestAppHostBackchannel();
                        backchannel.RequestStopAsyncCalled = publishModeCompleted;
                        backchannelCompletionSource?.SetResult(backchannel);
                        await publishModeCompleted.Task;
                        return 0; // Simulate successful run
                    }
                };

                return runner;
            };

            options.PublishCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestPublishCommandPrompter(interactionService);
                return prompter;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("publish");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode); // Ensure the command succeeds
    }
}

internal sealed class TestPublishCommandPrompter(IInteractionService interactionService) : PublishCommandPrompter(interactionService)
{
    public Func<IEnumerable<string>, string>? PromptForPublisherCallback { get; set; }

    public override Task<string> PromptForPublisherAsync(IEnumerable<string> publishers, CancellationToken cancellationToken)
    {
        return PromptForPublisherCallback switch
        {
            { } callback => Task.FromResult(callback(publishers)),
            _ => Task.FromResult(publishers.First()) // Default to the first publisher if no callback is provided.
        };
    }
}
