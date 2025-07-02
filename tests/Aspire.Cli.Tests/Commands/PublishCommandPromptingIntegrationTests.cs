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
using Spectre.Console;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class PublishCommandPromptingIntegrationTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task PublishCommand_TextInputPrompt_SendsCorrectKeyPresses()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new TestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up the prompt that will be sent from AppHost
        promptBackchannel.AddPrompt("text-prompt-1", "Environment Name", InputTypes.Text, "Enter environment name:", isRequired: true);

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

        // Verify the prompt was received and response was sent
        Assert.Single(promptBackchannel.ReceivedPrompts);
        var receivedPrompt = promptBackchannel.ReceivedPrompts[0];
        Assert.Equal("text-prompt-1", receivedPrompt.PromptId);
        Assert.Equal("Environment Name", receivedPrompt.Inputs[0].Label);
        Assert.Equal(InputTypes.Text, receivedPrompt.Inputs[0].InputType);

        // Verify the correct response was sent back
        Assert.Single(promptBackchannel.CompletedPrompts);
        var completedPrompt = promptBackchannel.CompletedPrompts[0];
        Assert.Equal("text-prompt-1", completedPrompt.PromptId);
        Assert.Equal("production", completedPrompt.Answers[0]);
    }

    [Fact]
    public async Task PublishCommand_SecretTextPrompt_SendsCorrectKeyPresses()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new TestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up the prompt that will be sent from AppHost
        promptBackchannel.AddPrompt("secret-prompt-1", "Database Password", InputTypes.SecretText, "Enter secure password:", isRequired: true);

        // Set up the expected user response
        consoleService.SetupStringPromptResponse("SecurePassword123!");

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

        // Verify the secret prompt was handled correctly
        Assert.Single(promptBackchannel.ReceivedPrompts);
        var receivedPrompt = promptBackchannel.ReceivedPrompts[0];
        Assert.Equal("secret-prompt-1", receivedPrompt.PromptId);
        Assert.Equal("Database Password", receivedPrompt.Inputs[0].Label);
        Assert.Equal(InputTypes.SecretText, receivedPrompt.Inputs[0].InputType);

        // Verify the correct response was sent back
        Assert.Single(promptBackchannel.CompletedPrompts);
        var completedPrompt = promptBackchannel.CompletedPrompts[0];
        Assert.Equal("secret-prompt-1", completedPrompt.PromptId);
        Assert.Equal("SecurePassword123!", completedPrompt.Answers[0]);
    }

    [Fact]
    public async Task PublishCommand_ChoicePrompt_SendsCorrectSelection()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new TestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up the choice prompt with options
        var options = new List<KeyValuePair<string, string>>
        {
            new("us-west-2", "US West (Oregon)"),
            new("us-east-1", "US East (N. Virginia)"),
            new("eu-central-1", "Europe (Frankfurt)")
        };
        promptBackchannel.AddPrompt("choice-prompt-1", "Deployment Region", InputTypes.Choice, "Select region:", isRequired: true, options: options);

        // Set up the expected user selection (by value)
        consoleService.SetupSelectionResponse("US East (N. Virginia)");

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

        // Verify the choice prompt was received correctly
        Assert.Single(promptBackchannel.ReceivedPrompts);
        var receivedPrompt = promptBackchannel.ReceivedPrompts[0];
        Assert.Equal("choice-prompt-1", receivedPrompt.PromptId);
        Assert.Equal("Deployment Region", receivedPrompt.Inputs[0].Label);
        Assert.Equal(InputTypes.Choice, receivedPrompt.Inputs[0].InputType);
        Assert.Equal(3, receivedPrompt.Inputs[0].Options?.Count);

        // Verify the correct selection was sent back
        Assert.Single(promptBackchannel.CompletedPrompts);
        var completedPrompt = promptBackchannel.CompletedPrompts[0];
        Assert.Equal("choice-prompt-1", completedPrompt.PromptId);
        Assert.Equal("us-east-1", completedPrompt.Answers[0]);
    }

    [Fact]
    public async Task PublishCommand_BooleanPrompt_SendsCorrectAnswer()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new TestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up the boolean prompt
        promptBackchannel.AddPrompt("bool-prompt-1", "Enable Verbose Logging", InputTypes.Boolean, "Enable verbose logging?", isRequired: false);

        // Set up the expected user response
        consoleService.SetupBooleanResponse(true);

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

        // Verify the boolean prompt was handled
        Assert.Single(promptBackchannel.ReceivedPrompts);
        var receivedPrompt = promptBackchannel.ReceivedPrompts[0];
        Assert.Equal("bool-prompt-1", receivedPrompt.PromptId);
        Assert.Equal("Enable Verbose Logging", receivedPrompt.Inputs[0].Label);
        Assert.Equal(InputTypes.Boolean, receivedPrompt.Inputs[0].InputType);

        // Verify the correct boolean response was sent back
        Assert.Single(promptBackchannel.CompletedPrompts);
        var completedPrompt = promptBackchannel.CompletedPrompts[0];
        Assert.Equal("bool-prompt-1", completedPrompt.PromptId);
        Assert.Equal("true", completedPrompt.Answers[0]);
    }

    [Fact]
    public async Task PublishCommand_NumberPrompt_SendsCorrectNumericValue()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new TestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up the number prompt
        promptBackchannel.AddPrompt("number-prompt-1", "Instance Count", InputTypes.Number, "Enter number of instances:", isRequired: true);

        // Set up the expected user response
        consoleService.SetupStringPromptResponse("3");

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

        // Verify the number prompt was handled
        Assert.Single(promptBackchannel.ReceivedPrompts);
        var receivedPrompt = promptBackchannel.ReceivedPrompts[0];
        Assert.Equal("number-prompt-1", receivedPrompt.PromptId);
        Assert.Equal("Instance Count", receivedPrompt.Inputs[0].Label);
        Assert.Equal(InputTypes.Number, receivedPrompt.Inputs[0].InputType);

        // Verify the correct numeric response was sent back
        Assert.Single(promptBackchannel.CompletedPrompts);
        var completedPrompt = promptBackchannel.CompletedPrompts[0];
        Assert.Equal("number-prompt-1", completedPrompt.PromptId);
        Assert.Equal("3", completedPrompt.Answers[0]);
    }

    [Fact]
    public async Task PublishCommand_MultiplePrompts_HandlesSequentialInteractions()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new TestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up multiple prompts that will be sent in sequence
        promptBackchannel.AddPrompt("text-prompt-1", "Application Name", InputTypes.Text, "Enter app name:", isRequired: true);
        promptBackchannel.AddPrompt("choice-prompt-1", "Environment", InputTypes.Choice, "Select environment:", isRequired: true,
            options:
            [
                new("dev", "Development"),
                new("staging", "Staging"),
                new("prod", "Production")
            ]);
        promptBackchannel.AddPrompt("bool-prompt-1", "Create Backup", InputTypes.Boolean, "Create backup?", isRequired: false);

        // Set up the expected user responses in order
        consoleService.SetupSequentialResponses(
            ("MyTestApp", ResponseType.String),
            ("Production", ResponseType.Selection),
            ("true", ResponseType.Boolean)
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

        // Verify all prompts were received in the correct order
        Assert.Equal(3, promptBackchannel.ReceivedPrompts.Count);

        var textPrompt = promptBackchannel.ReceivedPrompts[0];
        Assert.Equal("text-prompt-1", textPrompt.PromptId);
        Assert.Equal("Application Name", textPrompt.Inputs[0].Label);
        Assert.Equal(InputTypes.Text, textPrompt.Inputs[0].InputType);

        var choicePrompt = promptBackchannel.ReceivedPrompts[1];
        Assert.Equal("choice-prompt-1", choicePrompt.PromptId);
        Assert.Equal("Environment", choicePrompt.Inputs[0].Label);
        Assert.Equal(InputTypes.Choice, choicePrompt.Inputs[0].InputType);

        var boolPrompt = promptBackchannel.ReceivedPrompts[2];
        Assert.Equal("bool-prompt-1", boolPrompt.PromptId);
        Assert.Equal("Create Backup", boolPrompt.Inputs[0].Label);
        Assert.Equal(InputTypes.Boolean, boolPrompt.Inputs[0].InputType);

        // Verify all responses were sent back correctly
        Assert.Equal(3, promptBackchannel.CompletedPrompts.Count);

        Assert.Equal("text-prompt-1", promptBackchannel.CompletedPrompts[0].PromptId);
        Assert.Equal("MyTestApp", promptBackchannel.CompletedPrompts[0].Answers[0]);

        Assert.Equal("choice-prompt-1", promptBackchannel.CompletedPrompts[1].PromptId);
        Assert.Equal("prod", promptBackchannel.CompletedPrompts[1].Answers[0]);

        Assert.Equal("bool-prompt-1", promptBackchannel.CompletedPrompts[2].PromptId);
        Assert.Equal("true", promptBackchannel.CompletedPrompts[2].Answers[0]);
    }

    [Fact]
    public async Task PublishCommand_SinglePromptWithMultipleInputs_HandlesAllInputs()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new TestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up a single prompt with multiple inputs
        promptBackchannel.AddMultiInputPrompt("multi-input-prompt-1", "Configuration Setup", "Please provide the following details:",
            [
                new("Database Connection String", InputTypes.Text, true, null),
                new("API Key", InputTypes.SecretText, true, null),
                new("Environment", InputTypes.Choice, true,
                [
                    new("dev", "Development"),
                    new("staging", "Staging"),
                    new("prod", "Production")
                ]),
                new("Enable Logging", InputTypes.Boolean, false, null)
            ]);

        // Set up the expected user responses for all inputs
        consoleService.SetupSequentialResponses(
            ("Server=localhost;Database=MyApp;", ResponseType.String),
            ("secret-api-key-12345", ResponseType.String),
            ("Staging", ResponseType.Selection),
            ("true", ResponseType.Boolean)
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

        // Verify that a single prompt with multiple inputs was received
        Assert.Single(promptBackchannel.ReceivedPrompts);
        var receivedPrompt = promptBackchannel.ReceivedPrompts[0];
        Assert.Equal("multi-input-prompt-1", receivedPrompt.PromptId);
        Assert.Equal("Please provide the following details:", receivedPrompt.Message);
        Assert.Equal(4, receivedPrompt.Inputs.Count);

        // Verify each input was configured correctly
        Assert.Equal("Database Connection String", receivedPrompt.Inputs[0].Label);
        Assert.Equal(InputTypes.Text, receivedPrompt.Inputs[0].InputType);
        Assert.True(receivedPrompt.Inputs[0].IsRequired);

        Assert.Equal("API Key", receivedPrompt.Inputs[1].Label);
        Assert.Equal(InputTypes.SecretText, receivedPrompt.Inputs[1].InputType);
        Assert.True(receivedPrompt.Inputs[1].IsRequired);

        Assert.Equal("Environment", receivedPrompt.Inputs[2].Label);
        Assert.Equal(InputTypes.Choice, receivedPrompt.Inputs[2].InputType);
        Assert.True(receivedPrompt.Inputs[2].IsRequired);
        Assert.Equal(3, receivedPrompt.Inputs[2].Options?.Count);

        Assert.Equal("Enable Logging", receivedPrompt.Inputs[3].Label);
        Assert.Equal(InputTypes.Boolean, receivedPrompt.Inputs[3].InputType);
        Assert.False(receivedPrompt.Inputs[3].IsRequired);

        // Verify that all responses were sent back correctly as a single array
        Assert.Single(promptBackchannel.CompletedPrompts);
        var completedPrompt = promptBackchannel.CompletedPrompts[0];
        Assert.Equal("multi-input-prompt-1", completedPrompt.PromptId);
        Assert.Equal(4, completedPrompt.Answers.Length);
        Assert.Equal("Server=localhost;Database=MyApp;", completedPrompt.Answers[0]);
        Assert.Equal("secret-api-key-12345", completedPrompt.Answers[1]);
        Assert.Equal("staging", completedPrompt.Answers[2]);
        Assert.Equal("true", completedPrompt.Answers[3]);
    }

    [Fact]
    public async Task PublishCommand_TextInputWithDefaultValue_UsesDefaultCorrectly()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var promptBackchannel = new TestPromptBackchannel();
        var consoleService = new TestConsoleInteractionServiceWithPromptTracking();

        // Set up the prompt with a default value
        promptBackchannel.AddPrompt("text-prompt-1", "Environment Name", InputTypes.Text, "Enter environment name:", isRequired: true, defaultValue: "development");

        // Set up the expected user response (they accept the default by providing the same value)
        consoleService.SetupStringPromptResponse("development");

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

        // Verify the prompt was handled correctly and includes the default value
        Assert.Single(promptBackchannel.ReceivedPrompts);
        var receivedPrompt = promptBackchannel.ReceivedPrompts[0];
        Assert.Equal("text-prompt-1", receivedPrompt.PromptId);
        Assert.Equal("Environment Name", receivedPrompt.Inputs[0].Label);
        Assert.Equal(InputTypes.Text, receivedPrompt.Inputs[0].InputType);
        Assert.Equal("development", receivedPrompt.Inputs[0].Value); // Check that the default value is present

        // Verify the correct response was sent back
        Assert.Single(promptBackchannel.CompletedPrompts);
        var completedPrompt = promptBackchannel.CompletedPrompts[0];
        Assert.Equal("text-prompt-1", completedPrompt.PromptId);
        Assert.Equal("development", completedPrompt.Answers[0]);

        // Verify that the PromptForStringAsync was called with the default value
        var promptCalls = consoleService.StringPromptCalls;
        Assert.Single(promptCalls);
        Assert.Equal("development", promptCalls[0].DefaultValue); // This verifies that our change works
    }

    private static TestDotNetCliRunner CreateTestRunnerWithPromptBackchannel(TestPromptBackchannel promptBackchannel)
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
            await promptBackchannel.WaitForCompletion();
            return 0;
        };

        return runner;
    }
}

// Test implementation of IAppHostBackchannel that simulates prompt interactions
internal sealed class TestPromptBackchannel : IAppHostBackchannel
{
    private readonly List<PromptData> _promptsToSend = [];
    private readonly TaskCompletionSource _completionSource = new();
    private readonly Dictionary<string, TaskCompletionSource> _promptCompletionSources = new();

    public List<PromptData> ReceivedPrompts { get; } = [];
    public List<PromptCompletion> CompletedPrompts { get; } = [];

    public void AddPrompt(string promptId, string label, string inputType, string message, bool isRequired, IReadOnlyList<KeyValuePair<string, string>>? options = null, string? defaultValue = null)
    {
        _promptsToSend.Add(new PromptData(promptId, [new PromptInputData(label, inputType, isRequired, options, defaultValue)], message));
    }

    public void AddMultiInputPrompt(string promptId, string title, string message, IReadOnlyList<PromptInputData> inputs)
    {
        _promptsToSend.Add(new PromptData(promptId, inputs, message, title));
    }

    public Task WaitForCompletion() => _completionSource.Task;

    public async IAsyncEnumerable<PublishingActivity> GetPublishingActivitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var prompt in _promptsToSend)
        {
            ReceivedPrompts.Add(prompt);

            var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _promptCompletionSources[prompt.PromptId] = completionSource;

            var inputs = prompt.Inputs.Select(input => new PublishingPromptInput
            {
                Label = input.Label,
                InputType = input.InputType,
                Required = input.IsRequired,
                Options = input.Options,
                Value = input.Value
            }).ToList();

            yield return new PublishingActivity
            {
                Type = PublishingActivityTypes.Prompt,
                Data = new PublishingActivityData
                {
                    Id = prompt.PromptId,
                    StatusText = prompt.Inputs.Count > 1
                        ? prompt.Title ?? prompt.Message
                        : prompt.Inputs[0].Label,
                    CompletionState = CompletionStates.InProgress,
                    StepId = "publish-step",
                    Inputs = inputs
                }
            };

            await completionSource.Task.WaitAsync(cancellationToken);
        }

        _completionSource.SetResult();
    }

    public Task CompletePromptResponseAsync(string promptId, string?[] answers, CancellationToken cancellationToken)
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
    public Task<long> PingAsync(long timestamp, CancellationToken cancellationToken) => Task.FromResult(timestamp);
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
}

// Data structures for tracking prompts
internal sealed record PromptInputData(string Label, string InputType, bool IsRequired, IReadOnlyList<KeyValuePair<string, string>>? Options = null, string? Value = null);
internal sealed record PromptData(string PromptId, IReadOnlyList<PromptInputData> Inputs, string Message, string? Title = null);
internal sealed record PromptCompletion(string PromptId, string?[] Answers);

// Enhanced TestConsoleInteractionService that tracks interaction types
[SuppressMessage("Usage", "ASPIREINTERACTION001:Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
internal sealed class TestConsoleInteractionServiceWithPromptTracking : IInteractionService
{
    private readonly Queue<(string response, ResponseType type)> _responses = new();
    private bool _shouldCancel;
    
    public List<StringPromptCall> StringPromptCalls { get; } = [];
    public List<object> SelectionPromptCalls { get; } = []; // Using object to handle generic types
    public List<BooleanPromptCall> BooleanPromptCalls { get; } = [];
    
    public void SetupStringPromptResponse(string response) => _responses.Enqueue((response, ResponseType.String));
    public void SetupSelectionResponse(string response) => _responses.Enqueue((response, ResponseType.Selection));
    public void SetupBooleanResponse(bool response) => _responses.Enqueue((response.ToString().ToLower(), ResponseType.Boolean));
    public void SetupCancellationResponse() => _shouldCancel = true;

    public void SetupSequentialResponses(params (string response, ResponseType type)[] responses)
    {
        foreach (var (response, type) in responses)
        {
            _responses.Enqueue((response, type));
        }
    }

    public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, ValidationResult>? validator = null, bool isSecret = false, bool required = false, CancellationToken cancellationToken = default)
    {
        StringPromptCalls.Add(new StringPromptCall(promptText, defaultValue, isSecret));
        
        if (_shouldCancel || cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }

        if (_responses.TryDequeue(out var response))
        {
            return Task.FromResult(response.response);
        }

        return Task.FromResult(defaultValue ?? string.Empty);
    }

    public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
    {
        if (_shouldCancel || cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }

        if (_responses.TryDequeue(out var response))
        {
            // Find the choice that matches the response
            var matchingChoice = choices.FirstOrDefault(c => choiceFormatter(c) == response.response || c.ToString() == response.response);
            if (matchingChoice != null)
            {
                return Task.FromResult(matchingChoice);
            }
        }

        return Task.FromResult(choices.First());
    }

    public Task<bool> ConfirmAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
    {
        BooleanPromptCalls.Add(new BooleanPromptCall(promptText, defaultValue));
        
        if (_shouldCancel || cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }

        if (_responses.TryDequeue(out var response))
        {
            return Task.FromResult(bool.Parse(response.response));
        }

        return Task.FromResult(defaultValue);
    }

    // Default implementations for other interface methods
    public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action) => action();
    public void ShowStatus(string statusText, Action action) => action();
    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingVersion) => 0;
    public void DisplayError(string errorMessage) { }
    public void DisplayMessage(string emoji, string message) { }
    public void DisplaySuccess(string message) { }
    public void DisplaySubtleMessage(string message) { }
    public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls) { }
    public void DisplayLines(IEnumerable<(string Stream, string Line)> lines) { }
    public void DisplayCancellationMessage() { }
    public void DisplayEmptyLine() { }
    public void DisplayPlainText(string text) { }

    public void DisplayVersionUpdateNotification(string newerVersion) { }
}

internal enum ResponseType
{
    String,
    Selection,
    Boolean
}

// Input type constants that match the Aspire CLI implementation
internal static class InputTypes
{
    public const string Text = "text";
    public const string SecretText = "secret-text";
    public const string Choice = "choice";
    public const string Boolean = "boolean";
    public const string Number = "number";
}

internal sealed record StringPromptCall(string PromptText, string? DefaultValue, bool IsSecret);
internal sealed record SelectionPromptCall<T>(string PromptText, IEnumerable<T> Choices, Func<T, string> ChoiceFormatter);
internal sealed record BooleanPromptCall(string PromptText, bool DefaultValue);
