// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.Orchestrator;
using Aspire.Hosting.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Equal(param.Value, await param.WaitForValueTcs.Task);
#pragma warning restore CS0618 // Type or member is obsolete
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
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Equal(param.Value, await param.WaitForValueTcs.Task);
#pragma warning restore CS0618 // Type or member is obsolete
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
        Assert.Equal("Unresolved parameters", messageBarInteraction.Title);
        Assert.Equal("There are unresolved parameters that need to be set. Please provide values for them.", messageBarInteraction.Message);

        // Complete the message bar interaction to proceed to inputs dialog
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true)); // Data = true (user clicked Enter Values)

        // Wait for the inputs interaction
        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Set unresolved parameters", inputsInteraction.Title);
        Assert.Equal("Please provide values for the unresolved parameters. Parameters can be saved to [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) for future use.", inputsInteraction.Message);
        Assert.True(inputsInteraction.Options!.EnableMessageMarkdown);

        Assert.Collection(inputsInteraction.Inputs,
            input =>
            {
                Assert.Equal("param1", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
                Assert.False(input.Required);
            },
            input =>
            {
                Assert.Equal("param2", input.Label);
                Assert.Equal(InputType.Text, input.InputType);
                Assert.False(input.Required);
            },
            input =>
            {
                Assert.Equal("secretParam", input.Label);
                Assert.Equal(InputType.SecretText, input.InputType);
                Assert.False(input.Required);
            },
            input =>
            {
                Assert.Equal("Save to user secrets", input.Label);
                Assert.Equal(InputType.Boolean, input.InputType);
                Assert.False(input.Required);
            });

        inputsInteraction.Inputs[0].Value = "value1";
        inputsInteraction.Inputs[1].Value = "value2";
        inputsInteraction.Inputs[2].Value = "secretValue";

        inputsInteraction.CompletionTcs.SetResult(InteractionResult.Ok(inputsInteraction.Inputs));

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
        Assert.Equal("Unresolved parameters", messageBarInteraction.Title);

        // Complete the message bar interaction with false (user chose not to enter values)
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Cancel<bool>());

        // Assert that the message bar will show up again if there are still unresolved parameters
        var nextMessageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal("Unresolved parameters", nextMessageBarInteraction.Title);

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

    [Fact]
    public async Task InitializeParametersAsync_WithMissingParameterValue_LogsWarningWithoutException()
    {
        // Arrange
        var loggerService = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var interactionService = CreateInteractionService();
        var parameterProcessor = CreateParameterProcessor(
            loggerService: loggerService,
            interactionService: interactionService);
        var parameterWithMissingValue = CreateParameterWithMissingValue("missingParam");

        // Set up log watching
        var logsTask = ConsoleLoggingTestHelpers.WatchForLogsAsync(loggerService, 1, parameterWithMissingValue);

        // Act
        await parameterProcessor.InitializeParametersAsync([parameterWithMissingValue]);

        // Wait for logs to be written
        var logs = await logsTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert - Should log warning without exception details
        Assert.Single(logs);
        var logEntry = logs[0];
        Assert.Contains("Parameter resource missingParam could not be initialized. Waiting for user input.", logEntry.Content);
        Assert.False(logEntry.IsErrorMessage);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithNonMissingParameterException_LogsErrorWithException()
    {
        // Arrange
        var loggerService = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var parameterProcessor = CreateParameterProcessor(loggerService: loggerService);
        var parameterWithError = CreateParameterWithGenericError("errorParam");

        // Set up log watching
        var logsTask = ConsoleLoggingTestHelpers.WatchForLogsAsync(loggerService, 1, parameterWithError);

        // Act
        await parameterProcessor.InitializeParametersAsync([parameterWithError]);

        // Wait for logs to be written
        var logs = await logsTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert - Should log error message
        Assert.Single(logs);
        var logEntry = logs[0];
        Assert.Contains("Failed to initialize parameter resource errorParam.", logEntry.Content);
        Assert.True(logEntry.IsErrorMessage);
    }

    [Fact]
    public async Task HandleUnresolvedParametersAsync_WithResolvedParameter_LogsResolutionViaInteraction()
    {
        // Arrange
        var loggerService = ConsoleLoggingTestHelpers.GetResourceLoggerService();
        var testInteractionService = new TestInteractionService();
        var notificationService = ResourceNotificationServiceTestHelpers.Create();
        var parameterProcessor = CreateParameterProcessor(
            notificationService: notificationService,
            loggerService: loggerService,
            interactionService: testInteractionService);
        var parameter = CreateParameterWithMissingValue("testParam");

        parameter.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Set up log watching
        var logsTask = ConsoleLoggingTestHelpers.WatchForLogsAsync(loggerService, 1, parameter);

        // Act - Start handling unresolved parameters
        var handleTask = parameterProcessor.HandleUnresolvedParametersAsync([parameter]);

        // Wait for the message bar interaction
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));

        // Wait for the inputs interaction
        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        inputsInteraction.Inputs[0].Value = "testValue";
        inputsInteraction.CompletionTcs.SetResult(InteractionResult.Ok(inputsInteraction.Inputs));

        // Wait for the handle task to complete
        await handleTask;

        // Wait for logs to be written
        var logs = await logsTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert - Should log that parameter was resolved via user interaction
        Assert.Single(logs);
        var logEntry = logs[0];
        Assert.Contains("Parameter resource testParam has been resolved via user interaction.", logEntry.Content);
        Assert.False(logEntry.IsErrorMessage);
    }

    [Fact]
    public async Task HandleUnresolvedParametersAsync_WithParameterDescriptions_CreatesInputsWithDescriptions()
    {
        // Arrange
        var testInteractionService = new TestInteractionService();
        var parameterProcessor = CreateParameterProcessor(interactionService: testInteractionService);

        var param1 = CreateParameterWithMissingValue("param1");
        param1.Description = "This is a test parameter";
        param1.EnableDescriptionMarkdown = false;

        var param2 = CreateParameterWithMissingValue("param2");
        param2.Description = "This parameter has **markdown** formatting";
        param2.EnableDescriptionMarkdown = true;

        List<ParameterResource> parameters = [param1, param2];

        foreach (var param in parameters)
        {
            param.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        // Act
        _ = parameterProcessor.HandleUnresolvedParametersAsync(parameters);

        // Wait for the message bar interaction and complete it
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));

        // Wait for the inputs interaction
        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();

        // Assert
        Assert.Equal(3, inputsInteraction.Inputs.Count); // 2 parameters + 1 save option

        var param1Input = inputsInteraction.Inputs[0];
        Assert.Equal("param1", param1Input.Label);
        Assert.Equal("This is a test parameter", param1Input.Description);
        Assert.False(param1Input.EnableDescriptionMarkdown);
        Assert.Equal(InputType.Text, param1Input.InputType);

        var param2Input = inputsInteraction.Inputs[1];
        Assert.Equal("param2", param2Input.Label);
        Assert.Equal("This parameter has **markdown** formatting", param2Input.Description);
        Assert.True(param2Input.EnableDescriptionMarkdown);
        Assert.Equal(InputType.Text, param2Input.InputType);
    }

    [Fact]
    public async Task HandleUnresolvedParametersAsync_WithSecretParameterWithDescription_CreatesSecretInput()
    {
        // Arrange
        var testInteractionService = new TestInteractionService();
        var parameterProcessor = CreateParameterProcessor(interactionService: testInteractionService);

        var secretParam = CreateParameterWithMissingValue("secretParam", secret: true);
        secretParam.Description = "This is a secret parameter";
        secretParam.EnableDescriptionMarkdown = false;

        List<ParameterResource> parameters = [secretParam];
        secretParam.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act
        _ = parameterProcessor.HandleUnresolvedParametersAsync(parameters);

        // Wait for the message bar interaction and complete it
        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));

        // Wait for the inputs interaction
        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();

        // Assert
        Assert.Equal(2, inputsInteraction.Inputs.Count); // 1 secret parameter + 1 save option

        var secretInput = inputsInteraction.Inputs[0];
        Assert.Equal("secretParam", secretInput.Label);
        Assert.Equal("This is a secret parameter", secretInput.Description);
        Assert.False(secretInput.EnableDescriptionMarkdown);
        Assert.Equal(InputType.SecretText, secretInput.InputType);
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
            logger ?? new NullLogger<ParameterProcessor>(),
            new DistributedApplicationOptions { DisableDashboard = disableDashboard }
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
}
