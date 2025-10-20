// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Tests.TestServices;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Cli.Utils;
using Aspire.TestUtilities;
using Aspire.Cli.Backchannel;
using Spectre.Console;

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
    public async Task DeployCommandSucceedsWithoutOutputPath()
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

                        // Verify that --output-path is NOT included when not specified
                        Assert.DoesNotContain("--output-path", args);

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
                        
                        // Verify the complete set of expected arguments for deploy command
                        Assert.Contains("--operation", args);
                        Assert.Contains("publish", args);
                        Assert.Contains("--publisher", args);
                        Assert.Contains("default", args);
                        Assert.Contains("true", args); // The value for --deploy flag

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
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/11217")]
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
                            // When output path is explicitly provided, it should be included
                            Assert.Contains("--output-path", args);
                            Assert.Contains("/tmp/test", args);

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
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("deploy --output-path /tmp/test");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DeployCommandDisplaysDefaultEnvironment()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        // Arrange
        var capturedMessages = new List<string>();
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();
            
            options.InteractionServiceFactory = (sp) =>
            {
                var testService = new TestConsoleInteractionService();
                // Capture DisplayMessage calls
                return new TestInteractionServiceWrapper(testService, capturedMessages);
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner
                {
                    BuildAsyncCallback = (projectFile, options, cancellationToken) => 0,
                    GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                    {
                        return (0, true, VersionHelper.GetDefaultTemplateVersion());
                    },
                    RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                    {
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
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("deploy");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains(capturedMessages, m => m.Contains("production"));
    }

    [Fact]
    public async Task DeployCommandDisplaysCustomEnvironment()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        // Arrange
        var capturedMessages = new List<string>();
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();
            
            options.InteractionServiceFactory = (sp) =>
            {
                var testService = new TestConsoleInteractionService();
                return new TestInteractionServiceWrapper(testService, capturedMessages);
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner
                {
                    BuildAsyncCallback = (projectFile, options, cancellationToken) => 0,
                    GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                    {
                        return (0, true, VersionHelper.GetDefaultTemplateVersion());
                    },
                    RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                    {
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
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("deploy --environment Staging");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains(capturedMessages, m => m.Contains("staging"));
    }

    [Fact]
    public async Task DeployCommandDisplaysEnvironmentWithEqualsFormat()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        // Arrange
        var capturedMessages = new List<string>();
        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();
            
            options.InteractionServiceFactory = (sp) =>
            {
                var testService = new TestConsoleInteractionService();
                return new TestInteractionServiceWrapper(testService, capturedMessages);
            };

            options.DotNetCliRunnerFactory = (sp) =>
            {
                var runner = new TestDotNetCliRunner
                {
                    BuildAsyncCallback = (projectFile, options, cancellationToken) => 0,
                    GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                    {
                        return (0, true, VersionHelper.GetDefaultTemplateVersion());
                    },
                    RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                    {
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
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("deploy --environment=Development");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains(capturedMessages, m => m.Contains("development"));
    }
}

/// <summary>
/// Wrapper around TestConsoleInteractionService that captures DisplayMessage calls
/// </summary>
internal sealed class TestInteractionServiceWrapper : IInteractionService
{
    private readonly IInteractionService _inner;
    private readonly List<string> _capturedMessages;

    public TestInteractionServiceWrapper(IInteractionService inner, List<string> capturedMessages)
    {
        _inner = inner;
        _capturedMessages = capturedMessages;
    }

    public void DisplayMessage(string emoji, string message)
    {
        _capturedMessages.Add(message);
        _inner.DisplayMessage(emoji, message);
    }

    public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action) => _inner.ShowStatusAsync(statusText, action);
    public void ShowStatus(string statusText, Action action) => _inner.ShowStatus(statusText, action);
    public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default) => _inner.PromptForStringAsync(promptText, defaultValue, validator, isSecret, required, cancellationToken);
    public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default) => _inner.ConfirmAsync(promptText, defaultValue, cancellationToken);
    public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull => _inner.PromptForSelectionAsync(promptText, choices, choiceFormatter, cancellationToken);
    public Task<IReadOnlyList<T>> PromptForSelectionsAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull => _inner.PromptForSelectionsAsync(promptText, choices, choiceFormatter, cancellationToken);
    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingVersion) => _inner.DisplayIncompatibleVersionError(ex, appHostHostingVersion);
    public void DisplayError(string errorMessage) => _inner.DisplayError(errorMessage);
    public void DisplayPlainText(string text) => _inner.DisplayPlainText(text);
    public void DisplayMarkdown(string markdown) => _inner.DisplayMarkdown(markdown);
    public void DisplaySuccess(string message) => _inner.DisplaySuccess(message);
    public void DisplaySubtleMessage(string message, bool escapeMarkup = true) => _inner.DisplaySubtleMessage(message, escapeMarkup);
    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines) => _inner.DisplayLines(lines);
    public void DisplayCancellationMessage() => _inner.DisplayCancellationMessage();
    public void DisplayEmptyLine() => _inner.DisplayEmptyLine();
    public void DisplayVersionUpdateNotification(string newerVersion) => _inner.DisplayVersionUpdateNotification(newerVersion);
    public void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false) => _inner.WriteConsoleLog(message, lineNumber, type, isErrorMessage);
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
