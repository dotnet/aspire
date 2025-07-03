// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.Orchestrator;
using Aspire.Hosting.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Channels;
using Xunit;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Aspire.Hosting.Tests.Orchestrator;

public class ParameterProcessorTests
{
    [Fact]
    public async Task InitializeParametersAsync_WithValidParameters_SetsActiveState()
    {
        // Arrange
        var parameterProcessor = CreateParameterProcessor();
        var parameters = new[]
        {
            CreateParameterResource("param1", "value1"),
            CreateParameterResource("param2", "value2")
        };

        // Act
        await parameterProcessor.InitializeParametersAsync(parameters);

        // Assert
        foreach (var param in parameters)
        {
            Assert.NotNull(param.WaitForValueTcs);
            Assert.True(param.WaitForValueTcs.Task.IsCompletedSuccessfully);
            Assert.Equal(param.Value, await param.WaitForValueTcs.Task);
        }
    }

    [Fact]
    public async Task InitializeParametersAsync_WithValidParametersAndDashboardEnabled_SetsActiveState()
    {
        // Arrange
        var interactionService = CreateInteractionService(disableDashboard: false);
        var parameterProcessor = CreateParameterProcessor(interactionService: interactionService, disableDashboard: false);
        var parameters = new[]
        {
            CreateParameterResource("param1", "value1"),
            CreateParameterResource("param2", "value2")
        };

        // Act
        await parameterProcessor.InitializeParametersAsync(parameters);

        // Assert
        foreach (var param in parameters)
        {
            Assert.NotNull(param.WaitForValueTcs);
            Assert.True(param.WaitForValueTcs.Task.IsCompletedSuccessfully);
            Assert.Equal(param.Value, await param.WaitForValueTcs.Task);
        }
    }

    [Fact]
    public async Task InitializeParametersAsync_WithSecretParameter_MarksAsSecret()
    {
        // Arrange
        var notificationService = ResourceNotificationServiceTestHelpers.Create();
        var parameterProcessor = CreateParameterProcessor(notificationService: notificationService);
        var secretParam = CreateParameterResource("secret", "secretValue", secret: true);

        var updates = new List<(IResource Resource, CustomResourceSnapshot Snapshot)>();
        var watchTask = Task.Run(async () =>
        {
            await foreach (var resourceEvent in notificationService.WatchAsync().ConfigureAwait(false))
            {
                updates.Add((resourceEvent.Resource, resourceEvent.Snapshot));
                break; // Only collect the first update
            }
        });

        // Act
        await parameterProcessor.InitializeParametersAsync([secretParam]);

        // Wait for the notification
        await watchTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        var (resource, snapshot) = Assert.Single(updates);
        Assert.Same(secretParam, resource);
        Assert.Equal(KnownResourceStates.Active, snapshot.State?.Text);
        Assert.Equal(KnownResourceStateStyles.Success, snapshot.State?.Style);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithMissingParameterValue_AddsToUnresolvedWhenInteractionAvailable()
    {
        // Arrange
        var interactionService = CreateInteractionService();
        var parameterProcessor = CreateParameterProcessor(interactionService: interactionService);
        var parameterWithMissingValue = CreateParameterWithMissingValue("missingParam");

        // Act
        await parameterProcessor.InitializeParametersAsync([parameterWithMissingValue]);

        // Assert
        Assert.NotNull(parameterWithMissingValue.WaitForValueTcs);
        Assert.False(parameterWithMissingValue.WaitForValueTcs.Task.IsCompleted);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithMissingParameterValue_SetsExceptionWhenInteractionNotAvailable()
    {
        // Arrange
        var interactionService = CreateInteractionService(disableDashboard: true);
        var parameterProcessor = CreateParameterProcessor(interactionService: interactionService);
        var parameterWithMissingValue = CreateParameterWithMissingValue("missingParam");

        // Act
        await parameterProcessor.InitializeParametersAsync([parameterWithMissingValue]);

        // Assert
        Assert.NotNull(parameterWithMissingValue.WaitForValueTcs);
        Assert.True(parameterWithMissingValue.WaitForValueTcs.Task.IsCompleted);
        Assert.True(parameterWithMissingValue.WaitForValueTcs.Task.IsFaulted);
        Assert.IsType<MissingParameterValueException>(parameterWithMissingValue.WaitForValueTcs.Task.Exception?.InnerException);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithMissingParameterValueAndDashboardEnabled_LeavesUnresolved()
    {
        // Arrange
        var interactionService = CreateInteractionService(disableDashboard: false);
        var parameterProcessor = CreateParameterProcessor(interactionService: interactionService, disableDashboard: false);
        var parameterWithMissingValue = CreateParameterWithMissingValue("missingParam");

        // Act
        await parameterProcessor.InitializeParametersAsync([parameterWithMissingValue]);

        // Assert - Parameter should remain unresolved when dashboard is enabled
        Assert.NotNull(parameterWithMissingValue.WaitForValueTcs);
        Assert.False(parameterWithMissingValue.WaitForValueTcs.Task.IsCompleted);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithNonMissingParameterException_SetsException()
    {
        // Arrange
        var parameterProcessor = CreateParameterProcessor();
        var parameterWithError = CreateParameterWithGenericError("errorParam");

        // Act
        await parameterProcessor.InitializeParametersAsync([parameterWithError]);

        // Assert
        Assert.NotNull(parameterWithError.WaitForValueTcs);
        Assert.True(parameterWithError.WaitForValueTcs.Task.IsCompleted);
        Assert.True(parameterWithError.WaitForValueTcs.Task.IsFaulted);
        Assert.IsType<InvalidOperationException>(parameterWithError.WaitForValueTcs.Task.Exception?.InnerException);
    }

    [Fact]
    public async Task HandleUnresolvedParametersAsync_WithMultipleUnresolvedParameters_CreatesInteractions()
    {
        // Arrange
        var testInteractionService = new TestInteractionService();
        var notificationService = ResourceNotificationServiceTestHelpers.Create();
        var parameterProcessor = CreateParameterProcessor(notificationService: notificationService, interactionService: testInteractionService);
        var param1 = CreateParameterWithMissingValue("param1");
        var param2 = CreateParameterWithMissingValue("param2");
        var secretParam = CreateParameterWithMissingValue("secretParam", secret: true);

        List<ParameterResource> parameters = [param1, param2, secretParam];

        foreach (var param in parameters)
        {
            // Initialize the parameters' WaitForValueTcs
            param.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        var updates = notificationService.WatchAsync().GetAsyncEnumerator();

        // Act - Start handling unresolved parameters
        var handleTask = parameterProcessor.HandleUnresolvedParametersAsync(parameters);

        // Assert - Wait for the first interaction (message bar)
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Unresolved Parameters", messageBarInteraction.Title);
        Assert.Equal("There are unresolved parameters that need to be set. Please provide values for them.", messageBarInteraction.Message);

        // Complete the message bar interaction to proceed to inputs dialog
        messageBarInteraction.CompletionTcs.SetResult(new InteractionResult<bool>(true, false)); // Data = true (user clicked Enter Values)

        // Wait for the inputs interaction
        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Set Unresolved Parameters", inputsInteraction.Title);
        Assert.Equal("Please provide values for the unresolved parameters.", inputsInteraction.Message);

        Assert.Collection(inputsInteraction.Inputs,
            input =>
            {
                Assert.Equal("param1", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
            },
            input =>
            {
                Assert.Equal("param2", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
            },
            input =>
            {
                Assert.Equal("secretParam", input.Label);
                Assert.Equal(InputType.SecretText, input.InputType);
            });

        inputsInteraction.Inputs[0].SetValue("value1");
        inputsInteraction.Inputs[1].SetValue("value2");
        inputsInteraction.Inputs[2].SetValue("secretValue");

        inputsInteraction.CompletionTcs.SetResult(new InteractionResult<IReadOnlyList<InteractionInput>>(inputsInteraction.Inputs, false));

        // Wait for the handle task to complete
        await handleTask;

        // Assert - All parameters should now be resolved
        Assert.True(param1.WaitForValueTcs!.Task.IsCompletedSuccessfully);
        Assert.True(param2.WaitForValueTcs!.Task.IsCompletedSuccessfully);
        Assert.True(secretParam.WaitForValueTcs!.Task.IsCompletedSuccessfully);
        Assert.Equal("value1", await param1.WaitForValueTcs.Task);
        Assert.Equal("value2", await param2.WaitForValueTcs.Task);
        Assert.Equal("secretValue", await secretParam.WaitForValueTcs.Task);

        // Notification service should have received updates for each parameter
        // Marking them as Active with the provided values
        await updates.MoveNextAsync();
        Assert.Equal(KnownResourceStates.Active, updates.Current.Snapshot.State?.Text);
        Assert.Equal("value1", updates.Current.Snapshot.Properties.FirstOrDefault(p => p.Name == KnownProperties.Parameter.Value)?.Value);

        await updates.MoveNextAsync();
        Assert.Equal(KnownResourceStates.Active, updates.Current.Snapshot.State?.Text);
        Assert.Equal("value2", updates.Current.Snapshot.Properties.FirstOrDefault(p => p.Name == KnownProperties.Parameter.Value)?.Value);

        await updates.MoveNextAsync();
        Assert.Equal(KnownResourceStates.Active, updates.Current.Snapshot.State?.Text);
        Assert.Equal("secretValue", updates.Current.Snapshot.Properties.FirstOrDefault(p => p.Name == KnownProperties.Parameter.Value)?.Value);
        Assert.True(updates.Current.Snapshot.Properties.FirstOrDefault(p => p.Name == KnownProperties.Parameter.Value)?.IsSensitive ?? false);
    }

    [Fact]
    public async Task HandleUnresolvedParametersAsync_WhenUserCancelsInteraction_ParametersRemainUnresolved()
    {
        // Arrange
        var testInteractionService = new TestInteractionService();
        var parameterProcessor = CreateParameterProcessor(interactionService: testInteractionService);
        var parameterWithMissingValue = CreateParameterWithMissingValue("missingParam");

        parameterWithMissingValue.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act - Start handling unresolved parameters
        _ = parameterProcessor.HandleUnresolvedParametersAsync([parameterWithMissingValue]);

        // Wait for the message bar interaction
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Unresolved Parameters", messageBarInteraction.Title);

        // Complete the message bar interaction with false (user chose not to enter values)
        messageBarInteraction.CompletionTcs.SetResult(new InteractionResult<bool>(false, false)); // Data = false (user dismissed/cancelled)

        // Assert that the message bar will show up again if there are still unresolved parameters
        var nextMessageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Unresolved Parameters", nextMessageBarInteraction.Title);

        // Assert - Parameter should remain unresolved since user cancelled
        Assert.NotNull(parameterWithMissingValue.WaitForValueTcs);
        Assert.False(parameterWithMissingValue.WaitForValueTcs.Task.IsCompleted);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithEmptyParameterList_CompletesSuccessfully()
    {
        // Arrange
        var parameterProcessor = CreateParameterProcessor();

        // Act & Assert - Should not throw
        await parameterProcessor.InitializeParametersAsync([]);
    }

    private static ParameterProcessor CreateParameterProcessor(
        ResourceNotificationService? notificationService = null,
        ResourceLoggerService? loggerService = null,
        IInteractionService? interactionService = null,
        ILogger<ParameterProcessor>? logger = null,
        bool disableDashboard = true)
    {
        return new ParameterProcessor(
            notificationService ?? ResourceNotificationServiceTestHelpers.Create(),
            loggerService ?? new ResourceLoggerService(),
            interactionService ?? CreateInteractionService(disableDashboard),
            logger ?? new NullLogger<ParameterProcessor>()
        );
    }

    private static InteractionService CreateInteractionService(bool disableDashboard = false)
    {
        return new InteractionService(
            new NullLogger<InteractionService>(),
            new DistributedApplicationOptions { DisableDashboard = disableDashboard },
            new ServiceCollection().BuildServiceProvider());
    }

    private static ParameterResource CreateParameterResource(string name, string value, bool secret = false)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"Parameters:{name}"] = value })
            .Build();

        return new ParameterResource(name, _ => configuration[$"Parameters:{name}"] ?? throw new MissingParameterValueException($"Parameter '{name}' is missing"), secret);
    }

    private static ParameterResource CreateParameterWithMissingValue(string name, bool secret = false)
    {
        return new ParameterResource(name, _ => throw new MissingParameterValueException($"Parameter '{name}' is missing"), secret: secret);
    }

    private static ParameterResource CreateParameterWithGenericError(string name)
    {
        return new ParameterResource(name, _ => throw new InvalidOperationException($"Generic error for parameter '{name}'"), secret: false);
    }

    private sealed record InteractionData(string Title, string? Message, IReadOnlyList<InteractionInput> Inputs, InteractionOptions? Options, TaskCompletionSource<object> CompletionTcs);

    private sealed class TestInteractionService : IInteractionService
    {
        public Channel<InteractionData> Interactions { get; } = Channel.CreateUnbounded<InteractionData>();

        public bool IsAvailable { get; set; } = true;

        public Task<InteractionResult<bool>> PromptConfirmationAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, string inputLabel, string placeHolder, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InteractionResult<InteractionInput>> PromptInputAsync(string title, string? message, InteractionInput input, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<InteractionResult<IReadOnlyList<InteractionInput>>> PromptInputsAsync(string title, string? message, IReadOnlyList<InteractionInput> inputs, InputsDialogInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            var data = new InteractionData(title, message, inputs, options, new TaskCompletionSource<object>());
            Interactions.Writer.TryWrite(data);
            return (InteractionResult<IReadOnlyList<InteractionInput>>)await data.CompletionTcs.Task;
        }

        public async Task<InteractionResult<bool>> PromptMessageBarAsync(string title, string message, MessageBarInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            var data = new InteractionData(title, message, [], options, new TaskCompletionSource<object>());
            Interactions.Writer.TryWrite(data);
            return (InteractionResult<bool>)await data.CompletionTcs.Task;
        }

        public Task<InteractionResult<bool>> PromptMessageBoxAsync(string title, string message, MessageBoxInteractionOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
