// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Tests.TestServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Tests.Commands;

public class DoCommandTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task DoCommandWithHelpArgumentReturnsZero()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper);
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("do --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DoCommandWithStepArgumentSucceeds()
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

                    // Simulate apphost running successfully and establishing a backchannel
                    RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                    {
                        Assert.True(options.NoLaunchProfile);

                        // Verify that the custom step is passed
                        Assert.Contains("--step", args);
                        Assert.Contains("my-custom-step", args);

                        var completed = new TaskCompletionSource();
                        var backchannel = new TestAppHostBackchannel
                        {
                            RequestStopAsyncCalled = completed
                        };
                        backchannelCompletionSource?.SetResult(backchannel);
                        await completed.Task;
                        return 0;
                    }
                };

                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("do my-custom-step");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DoCommandWithDeployStepSucceeds()
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
                    BuildAsyncCallback = (projectFile, options, cancellationToken) => 0,

                    GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                    {
                        return (0, true, VersionHelper.GetDefaultTemplateVersion());
                    },

                    RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                    {
                        // Verify that --step deploy is passed
                        Assert.Contains("--step", args);
                        Assert.Contains("deploy", args);

                        var completed = new TaskCompletionSource();
                        var backchannel = new TestAppHostBackchannel
                        {
                            RequestStopAsyncCalled = completed
                        };
                        backchannelCompletionSource?.SetResult(backchannel);
                        await completed.Task;
                        return 0;
                    }
                };

                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("do deploy");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DoCommandWithPublishStepSucceeds()
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
                    BuildAsyncCallback = (projectFile, options, cancellationToken) => 0,

                    GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                    {
                        return (0, true, VersionHelper.GetDefaultTemplateVersion());
                    },

                    RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                    {
                        // Verify that --step publish is passed
                        Assert.Contains("--step", args);
                        Assert.Contains("publish", args);

                        var completed = new TaskCompletionSource();
                        var backchannel = new TestAppHostBackchannel
                        {
                            RequestStopAsyncCalled = completed
                        };
                        backchannelCompletionSource?.SetResult(backchannel);
                        await completed.Task;
                        return 0;
                    }
                };

                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("do publish");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DoCommandPassesOutputPathWhenSpecified()
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
                    BuildAsyncCallback = (projectFile, options, cancellationToken) => 0,

                    GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
                    {
                        return (0, true, VersionHelper.GetDefaultTemplateVersion());
                    },

                    RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
                    {
                        // Verify output path is included
                        Assert.Contains("--output-path", args);
                        
                        // Find the output path argument value
                        var outputPathIndex = Array.IndexOf(args, "--output-path");
                        Assert.True(outputPathIndex >= 0 && outputPathIndex < args.Length - 1);
                        var outputPath = args[outputPathIndex + 1];
                        Assert.EndsWith("test-output", outputPath);

                        var completed = new TaskCompletionSource();
                        var backchannel = new TestAppHostBackchannel
                        {
                            RequestStopAsyncCalled = completed
                        };
                        backchannelCompletionSource?.SetResult(backchannel);
                        await completed.Task;
                        return 0;
                    }
                };

                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("do my-step --output-path test-output");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task DoCommandFailsWithInvalidProjectFile()
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
                        return (1, false, null);
                    }
                };
                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("do my-step --project invalid.csproj");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(ExitCodeConstants.FailedToFindProject, exitCode);
    }

    [Fact]
    public async Task DoCommandWithExtensionMode_PromptsForStepAndEnvironment()
    {
        using var tempRepo = TemporaryWorkspace.Create(outputHelper);

        // Arrange
        var stepCaptured = false;
        var environmentCaptured = false;

        var services = CliTestHelper.CreateServiceCollection(tempRepo, outputHelper, options =>
        {
            options.ConfigurationCallback += config =>
            {
                // Enable extension mode for testing
                config["ASPIRE_EXTENSION_PROMPT_ENABLED"] = "true";
                config["ASPIRE_EXTENSION_TOKEN"] = "token";
            };

            // Use a custom interaction service that returns valid test values
            options.InteractionServiceFactory = sp =>
            {
                var testService = new TestExtensionInteractionService(sp);
                // Wrap it to provide custom prompt behavior
                return new DoCommandTestInteractionService(testService);
            };
            options.ExtensionBackchannelFactory = sp => new TestExtensionBackchannel();

            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();

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
                        // Verify that step is passed (from prompt)
                        if (args.Contains("--step"))
                        {
                            var stepIndex = Array.IndexOf(args, "--step");
                            if (stepIndex >= 0 && stepIndex < args.Length - 1 && args[stepIndex + 1] == "test-step")
                            {
                                stepCaptured = true;
                            }
                        }

                        // Verify that environment is passed (from prompt)
                        if (args.Contains("--environment"))
                        {
                            var envIndex = Array.IndexOf(args, "--environment");
                            if (envIndex >= 0 && envIndex < args.Length - 1 && args[envIndex + 1] == "Production")
                            {
                                environmentCaptured = true;
                            }
                        }

                        var completed = new TaskCompletionSource();
                        var backchannel = new TestAppHostBackchannel
                        {
                            RequestStopAsyncCalled = completed
                        };
                        backchannelCompletionSource?.SetResult(backchannel);
                        await completed.Task;
                        return 0;
                    }
                };

                return runner;
            };
        });

        var provider = services.BuildServiceProvider();
        var command = provider.GetRequiredService<RootCommand>();

        // Act - invoke do without step argument (should trigger prompts)
        var result = command.Parse("do");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(stepCaptured, "Step 'test-step' should be captured from prompt");
        Assert.True(environmentCaptured, "Environment 'Production' should be captured from prompt");
    }

    // Wrapper interaction service for DoCommand testing that delegates to the test service but overrides prompts
    private sealed class DoCommandTestInteractionService(IExtensionInteractionService innerService) : IExtensionInteractionService
    {
        public IExtensionBackchannel Backchannel => innerService.Backchannel;

        public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action) => innerService.ShowStatusAsync(statusText, action);
        public void ShowStatus(string statusText, Action action) => innerService.ShowStatus(statusText, action);

        public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default)
        {
            // Return test-step for the step prompt
            if (promptText.Contains("step", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult("test-step");
            }
            return Task.FromResult(defaultValue ?? "test-value");
        }

        public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
        {
            // Delegate to inner service (returns first choice by default)
            return innerService.PromptForSelectionAsync(promptText, choices, choiceFormatter, cancellationToken);
        }

        public Task<IReadOnlyList<T>> PromptForSelectionsAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
        {
            return innerService.PromptForSelectionsAsync(promptText, choices, choiceFormatter, cancellationToken);
        }

        public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
        {
            // Return false for optional prompts (output path, log level)
            return Task.FromResult(false);
        }

        public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingVersion) => innerService.DisplayIncompatibleVersionError(ex, appHostHostingVersion);
        public void DisplayError(string errorMessage) => innerService.DisplayError(errorMessage);
        public void DisplayMessage(string emoji, string message) => innerService.DisplayMessage(emoji, message);
        public void DisplaySuccess(string message) => innerService.DisplaySuccess(message);
        public void DisplayDashboardUrls(DashboardUrlsState dashboardUrls) => innerService.DisplayDashboardUrls(dashboardUrls);
        public void NotifyAppHostStartupCompleted() => innerService.NotifyAppHostStartupCompleted();
        public void DisplayConsolePlainText(string message) => innerService.DisplayConsolePlainText(message);
        public Task StartDebugSessionAsync(string workingDirectory, string? projectFile, bool debug) => innerService.StartDebugSessionAsync(workingDirectory, projectFile, debug);
        public void WriteDebugSessionMessage(string message, bool stdout, string? textStyle) => innerService.WriteDebugSessionMessage(message, stdout, textStyle);
        public void DisplayVersionUpdateNotification(string newerVersion, string? updateCommand = null) => innerService.DisplayVersionUpdateNotification(newerVersion, updateCommand);
        public void DisplaySubtleMessage(string message, bool escapeMarkup = true) => innerService.DisplaySubtleMessage(message, escapeMarkup);
        public void WriteConsoleLog(string message, int? lineNumber = null, string? type = null, bool isErrorMessage = false) => innerService.WriteConsoleLog(message, lineNumber, type, isErrorMessage);
        public void OpenEditor(string filePath) => innerService.OpenEditor(filePath);
        public void DisplayPlainText(string text) => innerService.DisplayPlainText(text);
        public void DisplayMarkdown(string markdown) => innerService.DisplayMarkdown(markdown);
        public void DisplayMarkupLine(string markup) => innerService.DisplayMarkupLine(markup);
        public void DisplayLines(IEnumerable<(string Stream, string Line)> lines) => innerService.DisplayLines(lines);
        public void DisplayCancellationMessage() => innerService.DisplayCancellationMessage();
        public void DisplayEmptyLine() => innerService.DisplayEmptyLine();
        public void DisplayRawText(string text) => innerService.DisplayRawText(text);
        public void ConsoleDisplaySubtleMessage(string message, bool escapeMarkup) => innerService.ConsoleDisplaySubtleMessage(message, escapeMarkup);
        public Task LaunchAppHostAsync(string workingDirectory, List<string> args, List<EnvVar> env, bool debug) => innerService.LaunchAppHostAsync(workingDirectory, args, env, debug);
        public void LogMessage(LogLevel level, string message) => innerService.LogMessage(level, message);
    }
}
