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

public class DeployCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task DeployCommandWithHelpArgumentReturnsZero()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("deploy --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DeployCommandFailsWithInvalidProjectFile()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        // Arrange
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper, options =>
        {
            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner
                {
                    GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                    {
                        return (1, false, null); // Simulate failure to retrieve app host information
                    }
                };
                return runner;
            };

            options.EnabledFeatureFlags = new[] { "deployCommandEnabled" }; // Ensure deploy command is enabled
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("deploy --project invalid.csproj");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task DeployCommandFailsWhenAppHostIsNotCompatible()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        // Arrange
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner
                {
                    GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                    {
                        return (0, false, "9.0.0"); // Simulate an incompatible app host
                    }
                };
                return runner;
            };

            options.EnabledFeatureFlags = new[] { "deployCommandEnabled" }; // Ensure deploy command is enabled
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("deploy --project valid.csproj");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(ExitCodeConstants.AppHostIncompatible, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task DeployCommandFailsWhenAppHostBuildFails()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        // Arrange
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner
                {
                    BuildAsyncCallback = (projectFile, options, cancellationToken) =>
                    {
                        return 1; // Simulate a build failure
                    }
                };
                return runner;
            };

            options.EnabledFeatureFlags = new[] { "deployCommandEnabled" }; // Ensure deploy command is enabled
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("deploy --project valid.csproj");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(ExitCodeConstants.FailedToBuildArtifacts, exitCode); // Ensure the command fails
    }

    [Fact]
    public async Task DeployCommandSucceedsEndToEnd()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        // Arrange
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner
                {
                    // Simulate a successful build
                    BuildAsyncCallback = (projectFile, options, cancellationToken) => 0,

                    // Simulate a successful app host information retrieval
                    GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                    {
                        return (0, true, VersionHelper.GetDefaultTemplateVersion()); // Compatible app host with backchannel support
                    },

                    // Simulate apphost running successfully and establishing a backchannel
                    RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                    {
                        Assert.True(options.NoLaunchProfile);

                        // Verify that the --deploy flag is included in the arguments
                        Assert.Contains("--deploy", args);

                        var deployModeCompleted = new TaskCompletionSource();
                        var backchannel = new TestAppHostBackchannel
                        {
                            RequestStopAsyncCalled = deployModeCompleted
                        };
                        backchannelCompletionSource?.SetResult(backchannel);
                        await deployModeCompleted.Task;
                        return 0; // Simulate successful run
                    }
                };

                return runner;
            };

            options.PublishCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestDeployCommandPrompter(interactionService);
                return prompter;
            };

            options.EnabledFeatureFlags = new[] { "deployCommandEnabled" }; // Ensure deploy command is enabled
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("deploy");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode); // Ensure the command succeeds
    }

    [Fact]
    public async Task DeployCommandIncludesDeployFlagInArguments()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        // Arrange
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner
                {
                    // Simulate a successful build
                    BuildAsyncCallback = (projectFile, options, cancellationToken) => 0,

                    // Simulate a successful app host information retrieval
                    GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                        {
                            return (0, true, VersionHelper.GetDefaultTemplateVersion());
                        },

                    // Simulate apphost running and verify --deploy flag is passed
                    RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                        {
                            // This is the key assertion - the deploy command should pass --deploy to the app host
                            Assert.Contains("--deploy", args);
                            Assert.Contains("--operation", args);
                            Assert.Contains("publish", args);
                            Assert.Contains("--publisher", args);
                            Assert.Contains("default", args);

                            var deployModeCompleted = new TaskCompletionSource();
                            var backchannel = new TestAppHostBackchannel
                            {
                                RequestStopAsyncCalled = deployModeCompleted
                            };
                            backchannelCompletionSource?.SetResult(backchannel);
                            await deployModeCompleted.Task;
                            return 0;
                        }
                };

                return runner;
            };

            options.PublishCommandPrompterFactory = (sp) =>
            {
                var interactionService = sp.GetRequiredService<IInteractionService>();
                var prompter = new TestDeployCommandPrompter(interactionService);
                return prompter;
            };

            options.EnabledFeatureFlags = new[] { "deployCommandEnabled" }; // Ensure deploy command is enabled
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("deploy --output-path /tmp/test");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
    }
}

internal sealed class TestDeployCommandPrompter(IInteractionService interactionService) : PublishCommandPrompter(interactionService)
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
