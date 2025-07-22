#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Commands;
using Aspire.Cli.Interaction;
using Aspire.Cli.Tests.TestServices;
using Aspire.Cli.Tests.Utils;
using Aspire.Cli.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Aspire.Cli.Tests.Commands;

public class PublishCommandPromptTextTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task PublishCommand_SingleInput_ShowsBothStatusTextAndLabel_WhenDifferent()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new CustomTestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up a single prompt where StatusText is different from Label
        // This simulates the real-world scenario where StatusText comes from PublishingActivityReporter
        promptBackchannel.AddPromptWithCustomStatusText(
            promptId: "single-prompt-1", 
            label: "Environment Name", 
            inputType: InputTypes.Text, 
            statusText: "Configure deployment target",
            isRequired: true);

        // Set up the expected user response
        consoleService.SetupStringPromptResponse("production");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();
            options.DotNetCliRunnerFactory = (sp) => CreateTestRunnerWithPromptBackchannel(promptBackchannel);
        });

        services.AddSingleton<IInteractionService>(consoleService);

        var serviceProvider = services.BuildServiceProvider();
        var command = serviceProvider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("publish");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);

        // Verify the prompt text shows both StatusText and Label when they are different
        var promptCalls = consoleService.StringPromptCalls;
        Assert.Single(promptCalls);
        
        var actualPromptText = promptCalls[0].PromptText;
        
        // With the fix, the prompt should show both StatusText and Label
        // Expected format: "[bold]Configure deployment target[/] Environment Name: "
        Assert.Contains("Configure deployment target", actualPromptText);
        Assert.Contains("Environment Name", actualPromptText);
        Assert.Contains("[bold]", actualPromptText);
        Assert.Contains("[/]", actualPromptText);
        Assert.EndsWith(": ", actualPromptText);
    }

    [Fact]
    public async Task PublishCommand_SingleInput_ShowsLabelOnly_WhenStatusTextEqualsLabel()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new TestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up a single prompt where StatusText equals Label
        promptBackchannel.AddPrompt("single-prompt-2", "Environment Name", InputTypes.Text, "Environment Name", isRequired: true);

        // Set up the expected user response
        consoleService.SetupStringPromptResponse("staging");

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();
            options.DotNetCliRunnerFactory = (sp) => CreateTestRunnerWithPromptBackchannel(promptBackchannel);
        });

        services.AddSingleton<IInteractionService>(consoleService);

        var serviceProvider = services.BuildServiceProvider();
        var command = serviceProvider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("publish");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);

        // Verify the prompt text shows only the label once when StatusText equals Label
        var promptCalls = consoleService.StringPromptCalls;
        Assert.Single(promptCalls);
        
        var actualPromptText = promptCalls[0].PromptText;
        
        // With the fix, expected format: "Environment Name: "
        // Current (broken) behavior: "[bold]Environment Name[/]"
        Assert.Equal("Environment Name: ", actualPromptText);
        
        // Should NOT contain bold markup since we're not duplicating
        Assert.DoesNotContain("[bold]", actualPromptText);
    }

    [Fact]
    public async Task PublishCommand_MultipleInputs_UsesLabelOnly_UnchangedBehavior()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new TestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up a multi-input prompt
        promptBackchannel.AddMultiInputPrompt("multi-input-prompt-1", "Configuration Setup", "Please provide the following details:",
            [
                new("Database Connection String", InputTypes.Text, true, null),
                new("API Key", InputTypes.SecretText, true, null)
            ]);

        // Set up the expected user responses
        consoleService.SetupSequentialResponses(
            ("Server=localhost;Database=MyApp;", ResponseType.String),
            ("secret-api-key-12345", ResponseType.String)
        );

        var services = CliTestHelper.CreateServiceCollection(workspace, outputHelper, options =>
        {
            options.ProjectLocatorFactory = (sp) => new TestProjectLocator();
            options.DotNetCliRunnerFactory = (sp) => CreateTestRunnerWithPromptBackchannel(promptBackchannel);
        });

        services.AddSingleton<IInteractionService>(consoleService);

        var serviceProvider = services.BuildServiceProvider();
        var command = serviceProvider.GetRequiredService<RootCommand>();

        // Act
        var result = command.Parse("publish");
        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);

        // Assert
        Assert.Equal(0, exitCode);

        // Verify that for multiple inputs, only the labels are used (unchanged behavior)
        var promptCalls = consoleService.StringPromptCalls;
        Assert.Equal(2, promptCalls.Count);
        
        // First input should be just the label with colon
        Assert.Equal("Database Connection String: ", promptCalls[0].PromptText);
        
        // Second input should be just the label with colon
        Assert.Equal("API Key: ", promptCalls[1].PromptText);
    }

    private static TestDotNetCliRunner CreateTestRunnerWithPromptBackchannel(IAppHostBackchannel promptBackchannel)
    {
        var runner = new TestDotNetCliRunner();

        // Simulate successful build
        runner.BuildAsyncCallback = (projectFile, options, cancellationToken) => 0;

        // Simulate compatible app host
        runner.GetAppHostInformationAsyncCallback = (projectFile, options, cancellationToken) =>
        {
            return (0, true, VersionHelper.GetDefaultTemplateVersion());
        };

        // Simulate successful app host run with the prompt backchannel
        runner.RunAsyncCallback = async (projectFile, watch, noBuild, args, env, backchannelCompletionSource, options, cancellationToken) =>
        {
            backchannelCompletionSource?.SetResult(promptBackchannel);
            if (promptBackchannel is CustomTestPromptBackchannel customBackchannel)
            {
                await customBackchannel.WaitForCompletion();
            }
            else if (promptBackchannel is TestPromptBackchannel testBackchannel)
            {
                await testBackchannel.WaitForCompletion();
            }
            return 0;
        };

        return runner;
    }
}

// Custom test backchannel that allows setting StatusText differently from Label
internal sealed class CustomTestPromptBackchannel : IAppHostBackchannel
{
    private readonly List<CustomPromptData> _customPrompts = [];
    private readonly TaskCompletionSource _completionSource = new();
    private readonly Dictionary<string, TaskCompletionSource> _promptCompletionSources = new();

    public List<PromptCompletion> CompletedPrompts { get; } = [];

    public void AddPromptWithCustomStatusText(string promptId, string label, string inputType, string statusText, bool isRequired, IReadOnlyList<KeyValuePair<string, string>>? options = null, string? defaultValue = null, IReadOnlyList<string>? validationErrors = null)
    {
        _customPrompts.Add(new CustomPromptData(promptId, label, inputType, statusText, isRequired, options, defaultValue, validationErrors));
    }

    public Task WaitForCompletion() => _completionSource.Task;

    public async IAsyncEnumerable<PublishingActivity> GetPublishingActivitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var prompt in _customPrompts)
        {
            var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _promptCompletionSources[prompt.PromptId] = completionSource;

            var input = new PublishingPromptInput
            {
                Label = prompt.Label,
                InputType = prompt.InputType,
                Required = prompt.IsRequired,
                Options = prompt.Options,
                Value = prompt.Value,
                ValidationErrors = prompt.ValidationErrors
            };

            yield return new PublishingActivity
            {
                Type = PublishingActivityTypes.Prompt,
                Data = new PublishingActivityData
                {
                    Id = prompt.PromptId,
                    StatusText = prompt.StatusText, // Use custom StatusText
                    CompletionState = CompletionStates.InProgress,
                    StepId = "publish-step",
                    Inputs = [input]
                }
            };

            await completionSource.Task.WaitAsync(cancellationToken);
        }

        _completionSource.SetResult();
    }

    public Task CompletePromptResponseAsync(string promptId, PublishingPromptInputAnswer[] answers, CancellationToken cancellationToken)
    {
        CompletedPrompts.Add(new PromptCompletion(promptId, answers));
        if (_promptCompletionSources.TryGetValue(promptId, out var completionSource))
        {
            completionSource.SetResult();
            _promptCompletionSources.Remove(promptId);
        }

        return Task.CompletedTask;
    }

    // Default implementations for other interface methods
    public Task RequestStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<(string, string?)>(("http://localhost:5000", null));
    public async IAsyncEnumerable<BackchannelLogEntry> GetAppHostLogEntriesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Suppress CS1998
        yield break;
    }
    public async IAsyncEnumerable<RpcResourceState> GetResourceStatesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Suppress CS1998
        yield break;
    }
    public Task ConnectAsync(string socketPath, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken) => Task.FromResult(new[] { "baseline.v2" });

    public async IAsyncEnumerable<CommandOutput> ExecAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Suppress CS1998
        yield break;
    }
}

internal sealed record CustomPromptData(string PromptId, string Label, string InputType, string StatusText, bool IsRequired, IReadOnlyList<KeyValuePair<string, string>>? Options = null, string? Value = null, IReadOnlyList<string>? ValidationErrors = null);

