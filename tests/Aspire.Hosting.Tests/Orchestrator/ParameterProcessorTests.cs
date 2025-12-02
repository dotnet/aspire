// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Dashboard.Model;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Pipelines.Internal;
using Aspire.Hosting.Resources;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Aspire.Hosting.Tests.Orchestrator;

public class ParameterProcessorTests
{
    [Fact]
    public async Task InitializeParametersAsync_WithValidParameters_SetsRunningState()
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
    public async Task InitializeParametersAsync_WithValidParametersAndDashboardEnabled_SetsRunningState()
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
        Assert.Equal(KnownResourceStates.Running, snapshot.State?.Text);
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
        Assert.Equal(InteractionStrings.ParametersBarTitle, messageBarInteraction.Title);
        Assert.Equal(InteractionStrings.ParametersBarMessage, messageBarInteraction.Message);

        // Complete the message bar interaction to proceed to inputs dialog
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true)); // Data = true (user clicked Enter Values)

        // Wait for the inputs interaction
        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal(InteractionStrings.ParametersInputsTitle, inputsInteraction.Title);
        Assert.Equal(InteractionStrings.ParametersInputsMessage, inputsInteraction.Message);
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
                Assert.Equal(InteractionStrings.ParametersInputsRememberLabel, input.Label);
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
        // Marking them as Running with the provided values
        await updates.MoveNextAsync();
        Assert.Equal(KnownResourceStates.Running, updates.Current.Snapshot.State?.Text);
        Assert.Equal("value1", updates.Current.Snapshot.Properties.FirstOrDefault(p => p.Name == KnownProperties.Parameter.Value)?.Value);

        await updates.MoveNextAsync();
        Assert.Equal(KnownResourceStates.Running, updates.Current.Snapshot.State?.Text);
        Assert.Equal("value2", updates.Current.Snapshot.Properties.FirstOrDefault(p => p.Name == KnownProperties.Parameter.Value)?.Value);

        await updates.MoveNextAsync();
        Assert.Equal(KnownResourceStates.Running, updates.Current.Snapshot.State?.Text);
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
        Assert.Equal(InteractionStrings.ParametersBarTitle, messageBarInteraction.Title);

        // Complete the message bar interaction with false (user chose not to enter values)
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Cancel<bool>());

        // Assert that the message bar will show up again if there are still unresolved parameters
        var nextMessageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        Assert.Equal(InteractionStrings.ParametersBarTitle, nextMessageBarInteraction.Title);

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

    [Fact]
    public async Task InitializeParametersAsync_WithDistributedApplicationModel_CollectsAndInitializesAllParameters()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var explicitParam = builder.AddParameter("explicitParam", () => "explicitValue");
        var referencedParam = builder.AddParameter("referencedParam", () => "referencedValue");

        // Create a container that references the parameter in an environment variable
        builder.AddContainer("testContainer", "nginx")
               .WithEnvironment("TEST_ENV", referencedParam);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterProcessor = CreateParameterProcessor();

        // Act
        await parameterProcessor.InitializeParametersAsync(model);

        // Assert
        var explicitParameterResource = model.Resources.OfType<ParameterResource>().First(p => p.Name == "explicitParam");
        var referencedParameterResource = model.Resources.OfType<ParameterResource>().First(p => p.Name == "referencedParam");

        Assert.NotNull(explicitParameterResource.WaitForValueTcs);
        Assert.NotNull(referencedParameterResource.WaitForValueTcs);
        Assert.True(explicitParameterResource.WaitForValueTcs.Task.IsCompletedSuccessfully);
        Assert.True(referencedParameterResource.WaitForValueTcs.Task.IsCompletedSuccessfully);
        Assert.Equal("explicitValue", await explicitParameterResource.WaitForValueTcs.Task);
        Assert.Equal("referencedValue", await referencedParameterResource.WaitForValueTcs.Task);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithDistributedApplicationModel_EmptyModel_CompletesSuccessfully()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterProcessor = CreateParameterProcessor();

        // Act & Assert - Should not throw
        await parameterProcessor.InitializeParametersAsync(model);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithDistributedApplicationModel_NoParameterReferences_InitializesExplicitOnly()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var explicitParam = builder.AddParameter("explicitParam", () => "explicitValue");

        // Add a container without parameter references
        builder.AddContainer("testContainer", "nginx")
               .WithEnvironment("TEST_ENV", "staticValue");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterProcessor = CreateParameterProcessor();

        // Act
        await parameterProcessor.InitializeParametersAsync(model);

        // Assert
        var explicitParameterResource = model.Resources.OfType<ParameterResource>().Single();
        Assert.Equal("explicitParam", explicitParameterResource.Name);
        Assert.NotNull(explicitParameterResource.WaitForValueTcs);
        Assert.True(explicitParameterResource.WaitForValueTcs.Task.IsCompletedSuccessfully);
        Assert.Equal("explicitValue", await explicitParameterResource.WaitForValueTcs.Task);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithDistributedApplicationModel_WithEnvironmentVariableReferences()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var referencedParam = builder.AddParameter("envParam", () => "envValue");

        builder.AddContainer("testContainer", "nginx")
               .WithEnvironment("TEST_ENV", referencedParam);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterProcessor = CreateParameterProcessor();

        // Act
        await parameterProcessor.InitializeParametersAsync(model);

        // Assert
        var parameterResource = model.Resources.OfType<ParameterResource>().Single();
        Assert.Equal("envParam", parameterResource.Name);
        Assert.NotNull(parameterResource.WaitForValueTcs);
        Assert.True(parameterResource.WaitForValueTcs.Task.IsCompletedSuccessfully);
        Assert.Equal("envValue", await parameterResource.WaitForValueTcs.Task);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithDistributedApplicationModel_WaitForResolution_True()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var param = builder.AddParameter("testParam", () => "testValue");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterProcessor = CreateParameterProcessor();

        // Act
        await parameterProcessor.InitializeParametersAsync(model, waitForResolution: true);

        // Assert
        var parameterResource = model.Resources.OfType<ParameterResource>().Single();
        Assert.NotNull(parameterResource.WaitForValueTcs);
        Assert.True(parameterResource.WaitForValueTcs.Task.IsCompletedSuccessfully);
        Assert.Equal("testValue", await parameterResource.WaitForValueTcs.Task);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithDistributedApplicationModel_WaitForResolution_False()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var param = builder.AddParameter("testParam", () => "testValue");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterProcessor = CreateParameterProcessor();

        // Act
        await parameterProcessor.InitializeParametersAsync(model, waitForResolution: false);

        // Assert
        var parameterResource = model.Resources.OfType<ParameterResource>().Single();
        Assert.NotNull(parameterResource.WaitForValueTcs);
        Assert.True(parameterResource.WaitForValueTcs.Task.IsCompletedSuccessfully);
        Assert.Equal("testValue", await parameterResource.WaitForValueTcs.Task);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithDistributedApplicationModel_WithMissingParameterValues_HandlesCorrectly()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var missingParam = builder.AddParameter("missingParam", () => throw new MissingParameterValueException("Parameter 'missingParam' is missing"));

        builder.AddContainer("testContainer", "nginx")
               .WithEnvironment("TEST_ENV", missingParam);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var interactionService = CreateInteractionService();
        var parameterProcessor = CreateParameterProcessor(interactionService: interactionService);

        // Act
        await parameterProcessor.InitializeParametersAsync(model);

        // Assert
        var parameterResource = model.Resources.OfType<ParameterResource>().Single();
        Assert.NotNull(parameterResource.WaitForValueTcs);
        Assert.False(parameterResource.WaitForValueTcs.Task.IsCompleted);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithDistributedApplicationModel_HandlesCircularReferences()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var param1 = builder.AddParameter("param1", () => "value1");
        var param2 = builder.AddParameter("param2", () => "value2");

        // Create a scenario that could potentially create circular references
        builder.AddContainer("container1", "nginx")
               .WithEnvironment("ENV1", param1)
               .WithEnvironment("ENV2", param2);

        builder.AddContainer("container2", "nginx")
               .WithEnvironment("ENV3", param1);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterProcessor = CreateParameterProcessor();

        // Act - Should not hang or throw due to circular references
        await parameterProcessor.InitializeParametersAsync(model);

        // Assert
        var parameters = model.Resources.OfType<ParameterResource>().ToList();
        Assert.Equal(2, parameters.Count);

        foreach (var param in parameters)
        {
            Assert.NotNull(param.WaitForValueTcs);
            Assert.True(param.WaitForValueTcs.Task.IsCompletedSuccessfully);
        }
    }

    [Fact]
    public async Task InitializeParametersAsync_UsesExecutionContextOptions_DoesNotThrow()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddSingleton<IDeploymentStateManager>(new MockDeploymentStateManager());
        var param = builder.AddParameter("testParam", () => "testValue");

        var serviceProviderAccessed = false;
        builder.AddContainer("testContainer", "nginx")
               .WithEnvironment(context =>
               {
                   // This should not throw InvalidOperationException
                   // when using the proper execution context constructor
                   var sp = context.ExecutionContext.ServiceProvider;
                   serviceProviderAccessed = sp is not null;
                   context.EnvironmentVariables["TEST_ENV"] = param;
               });

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Get the ParameterProcessor from the built app's service provider
        // This ensures it has the proper execution context with ServiceProvider
        var parameterProcessor = app.Services.GetRequiredService<ParameterProcessor>();

        // Act - Should not throw InvalidOperationException about IServiceProvider not being available
        await parameterProcessor.InitializeParametersAsync(model);

        // Assert
        Assert.True(serviceProviderAccessed);
        var parameterResource = model.Resources.OfType<ParameterResource>().Single();
        Assert.NotNull(parameterResource.WaitForValueTcs);
        Assert.True(parameterResource.WaitForValueTcs.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task InitializeParametersAsync_SkipsResourcesExcludedFromPublish()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var param = builder.AddParameter("excludedParam", () => "excludedValue");

        var excludedContainer = builder.AddContainer("excludedContainer", "nginx")
               .WithEnvironment(context =>
               {
                   context.EnvironmentVariables["EXCLUDED_ENV"] = param;
               });

        // Mark the container as excluded from publish
        excludedContainer.ExcludeFromManifest();

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var parameterProcessor = CreateParameterProcessor();

        // Act - The excluded container should be skipped during parameter collection
        await parameterProcessor.InitializeParametersAsync(model);

        // Assert
        // The environment callback should have been invoked during parameter collection
        // because we now create a publish execution context to collect dependent parameters
        // However, since we filter out excluded resources, the parameter should not be initialized
        // unless it's explicitly in the model
        var parameters = model.Resources.OfType<ParameterResource>().ToList();
        Assert.Single(parameters);

        var parameterResource = parameters[0];
        Assert.Equal("excludedParam", parameterResource.Name);

        // The parameter should be initialized since it's explicitly in the model
        Assert.NotNull(parameterResource.WaitForValueTcs);
        Assert.True(parameterResource.WaitForValueTcs.Task.IsCompletedSuccessfully);
    }

    private static ParameterProcessor CreateParameterProcessor(
        ResourceNotificationService? notificationService = null,
        ResourceLoggerService? loggerService = null,
        IInteractionService? interactionService = null,
        ILogger<ParameterProcessor>? logger = null,
        bool disableDashboard = true,
        DistributedApplicationExecutionContext? executionContext = null,
        IDeploymentStateManager? deploymentStateManager = null)
    {
        return new ParameterProcessor(
            notificationService ?? ResourceNotificationServiceTestHelpers.Create(),
            loggerService ?? new ResourceLoggerService(),
            interactionService ?? CreateInteractionService(disableDashboard),
            logger ?? new NullLogger<ParameterProcessor>(),
            executionContext ?? new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            deploymentStateManager ?? new MockDeploymentStateManager()
        );
    }

    private static InteractionService CreateInteractionService(bool disableDashboard = false)
    {
        return new InteractionService(
            new NullLogger<InteractionService>(),
            new DistributedApplicationOptions { DisableDashboard = disableDashboard },
            new ServiceCollection().BuildServiceProvider(),
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
    }

    private sealed class MockDeploymentStateManager : IDeploymentStateManager
    {
        public string? StateFilePath => null;

        public Task<DeploymentStateSection> AcquireSectionAsync(string sectionName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DeploymentStateSection(sectionName, [], 0));
        }

        public Task SaveSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
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

    [Fact]
    public async Task InitializeParametersAsync_WithGenerateParameterDefaultInPublishMode_DoesNotThrowWhenValueExists()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Parameters:generatedParam"] = "existingValue" })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var executionContext = new DistributedApplicationExecutionContext(
            new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish, "manifest")
            {
                ServiceProvider = serviceProvider
            });

        var parameterProcessor = CreateParameterProcessor(executionContext: executionContext);

        var parameterWithGenerateDefault = new ParameterResource(
            "generatedParam",
            parameterDefault => configuration["Parameters:generatedParam"] ?? parameterDefault?.GetDefaultValue() ?? throw new MissingParameterValueException("Parameter 'generatedParam' is missing"),
            secret: false)
        {
            Default = new GenerateParameterDefault()
        };

        // Act
        await parameterProcessor.InitializeParametersAsync([parameterWithGenerateDefault]);

        // Assert - Should succeed because value exists in configuration
        Assert.NotNull(parameterWithGenerateDefault.WaitForValueTcs);
        Assert.True(parameterWithGenerateDefault.WaitForValueTcs.Task.IsCompletedSuccessfully);
        Assert.Equal("existingValue", await parameterWithGenerateDefault.WaitForValueTcs.Task);
    }

    [Fact]
    public async Task ConnectionStringParameterStateIsSavedWithCorrectKey()
    {
        var capturingStateManager = new CapturingMockDeploymentStateManager();
        var testInteractionService = new TestInteractionService();
        var notificationService = ResourceNotificationServiceTestHelpers.Create();
        var parameterProcessor = CreateParameterProcessor(
            notificationService: notificationService,
            interactionService: testInteractionService,
            deploymentStateManager: capturingStateManager);

        var connectionStringParam = new ConnectionStringParameterResource(
            "mydb",
            _ => throw new MissingParameterValueException("Connection string 'mydb' is missing"),
            null);
        connectionStringParam.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        List<ParameterResource> parameters = [connectionStringParam];

        var handleTask = parameterProcessor.HandleUnresolvedParametersAsync(parameters);

        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));

        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        inputsInteraction.Inputs[0].Value = "Server=localhost;Database=mydb";
        inputsInteraction.Inputs[1].Value = "true";
        inputsInteraction.CompletionTcs.SetResult(InteractionResult.Ok(inputsInteraction.Inputs));

        await handleTask;

        // Verify the value was saved correctly in the flattened state
        Assert.True(capturingStateManager.State.TryGetPropertyValue("ConnectionStrings:mydb", out var valueNode));
        Assert.Equal("Server=localhost;Database=mydb", valueNode?.GetValue<string>());

        // Verify the entire state structure as JSON (mimics what gets saved to disk)
        await VerifyJson(capturingStateManager.State.ToJsonString());
    }

    [Fact]
    public async Task RegularParameterStateIsSavedWithCorrectKey()
    {
        var capturingStateManager = new CapturingMockDeploymentStateManager();
        var testInteractionService = new TestInteractionService();
        var notificationService = ResourceNotificationServiceTestHelpers.Create();
        var parameterProcessor = CreateParameterProcessor(
            notificationService: notificationService,
            interactionService: testInteractionService,
            deploymentStateManager: capturingStateManager);

        var regularParam = new ParameterResource(
            "myparam",
            _ => throw new MissingParameterValueException("Parameter 'myparam' is missing"),
            secret: false);
        regularParam.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        List<ParameterResource> parameters = [regularParam];

        var handleTask = parameterProcessor.HandleUnresolvedParametersAsync(parameters);

        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));

        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        inputsInteraction.Inputs[0].Value = "myvalue";
        inputsInteraction.Inputs[1].Value = "true";
        inputsInteraction.CompletionTcs.SetResult(InteractionResult.Ok(inputsInteraction.Inputs));

        await handleTask;

        // Verify the value was saved correctly in the flattened state
        Assert.True(capturingStateManager.State.TryGetPropertyValue("Parameters:myparam", out var valueNode));
        Assert.Equal("myvalue", valueNode?.GetValue<string>());

        // Verify the entire state structure as JSON (mimics what gets saved to disk)
        await VerifyJson(capturingStateManager.State.ToJsonString());
    }

    [Fact]
    public async Task CustomConfigurationKeyParameterStateIsSavedWithCorrectKey()
    {
        var capturingStateManager = new CapturingMockDeploymentStateManager();
        var testInteractionService = new TestInteractionService();
        var notificationService = ResourceNotificationServiceTestHelpers.Create();
        var parameterProcessor = CreateParameterProcessor(
            notificationService: notificationService,
            interactionService: testInteractionService,
            deploymentStateManager: capturingStateManager);

        var customParam = new ParameterResource(
            "customparam",
            _ => throw new MissingParameterValueException("Parameter 'customparam' is missing"),
            secret: false)
        {
            ConfigurationKey = "MyCustomSection:MyCustomKey"
        };
        customParam.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        List<ParameterResource> parameters = [customParam];

        var handleTask = parameterProcessor.HandleUnresolvedParametersAsync(parameters);

        var messageBarInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        messageBarInteraction.CompletionTcs.SetResult(InteractionResult.Ok(true));

        var inputsInteraction = await testInteractionService.Interactions.Reader.ReadAsync();
        inputsInteraction.Inputs[0].Value = "customvalue";
        inputsInteraction.Inputs[1].Value = "true";
        inputsInteraction.CompletionTcs.SetResult(InteractionResult.Ok(inputsInteraction.Inputs));

        await handleTask;

        // Verify the value was saved correctly in the flattened state
        Assert.True(capturingStateManager.State.TryGetPropertyValue("MyCustomSection:MyCustomKey", out var valueNode));
        Assert.Equal("customvalue", valueNode?.GetValue<string>());

        // Verify the entire state structure as JSON (mimics what gets saved to disk)
        await VerifyJson(capturingStateManager.State.ToJsonString());
    }

    private sealed class CapturingMockDeploymentStateManager : IDeploymentStateManager
    {
        // Stores the entire state in an unflattened structure in memory, then flattens for verification
        // to mimic FileDeploymentStateManager behavior
        private readonly JsonObject _unflattenedState = [];
        private JsonObject? _flattenedState;

        // Provides the flattened state for verification, matching what FileDeploymentStateManager saves to disk
        public JsonObject State => _flattenedState ?? [];
        public string? StateFilePath => null;

        public Task<DeploymentStateSection> AcquireSectionAsync(string sectionName, CancellationToken cancellationToken = default)
        {
            // Return existing section data if it exists, otherwise return empty
            var sectionData = _unflattenedState.TryGetPropertyValue(sectionName, out var sectionNode) && sectionNode is JsonObject obj
                ? obj.DeepClone().AsObject()
                : null;

            return Task.FromResult(new DeploymentStateSection(sectionName, sectionData, 0));
        }

        public Task SaveSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken = default)
        {
            // Increment version to allow multiple saves with the same instance (mimics FileDeploymentStateManager)
            section.Version++;

            // Store the section data in the unflattened state object
            _unflattenedState[section.SectionName] = section.Data.DeepClone().AsObject();

            // Flatten the state to mimic what FileDeploymentStateManager saves to disk
            _flattenedState = JsonFlattener.FlattenJsonObject(_unflattenedState);

            return Task.CompletedTask;
        }
    }
}
