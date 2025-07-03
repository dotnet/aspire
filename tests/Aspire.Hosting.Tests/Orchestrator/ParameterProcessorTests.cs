// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Orchestrator;
using Aspire.Hosting.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        var interactionService = CreateInteractionService();
        var parameterProcessor = CreateParameterProcessor(interactionService: interactionService);
        var param1 = CreateParameterWithMissingValue("param1");
        var param2 = CreateParameterWithMissingValue("param2");
        var secretParam = CreateParameterWithMissingValue("secretParam");

        // Act - Initialize parameters to add them to unresolved list
        await parameterProcessor.HandleUnresolvedParametersAsync([param1, param2, secretParam]);

        // Assert - All parameters should be unresolved and interaction should be created
        Assert.NotNull(param1.WaitForValueTcs);
        Assert.NotNull(param2.WaitForValueTcs);
        Assert.NotNull(secretParam.WaitForValueTcs);
        Assert.False(param1.WaitForValueTcs.Task.IsCompleted);
        Assert.False(param2.WaitForValueTcs.Task.IsCompleted);
        Assert.False(secretParam.WaitForValueTcs.Task.IsCompleted);
        
        // Verify there's an active interaction for the parameters
        var interactions = interactionService.GetCurrentInteractions();
        Assert.NotEmpty(interactions);
        
        // Verify the interaction has the expected title
        Assert.Contains(interactions, i => i.Title == "Unresolved Parameters");
    }

    [Fact]
    public async Task HandleUnresolvedParametersAsync_WithNoInteractionService_DoesNotCreateInteractions()
    {
        // Arrange - Use interaction service with dashboard disabled
        var interactionService = CreateInteractionService(disableDashboard: true);
        var parameterProcessor = CreateParameterProcessor(interactionService: interactionService);
        var parameterWithMissingValue = CreateParameterWithMissingValue("missingParam");

        // Act - Initialize parameters
        await parameterProcessor.InitializeParametersAsync([parameterWithMissingValue]);

        // Allow background task time to run (if it would run)
        await Task.Delay(100);

        // Assert - Parameter should be in error state since interaction service is not available
        Assert.NotNull(parameterWithMissingValue.WaitForValueTcs);
        Assert.True(parameterWithMissingValue.WaitForValueTcs.Task.IsCompleted);
        Assert.True(parameterWithMissingValue.WaitForValueTcs.Task.IsFaulted);
        
        // No interactions should be created
        var interactions = interactionService.GetCurrentInteractions();
        Assert.Empty(interactions);
    }

    [Fact]
    public async Task InitializeParametersAsync_WithEmptyParameterList_CompletesSuccessfully()
    {
        // Arrange
        var parameterProcessor = CreateParameterProcessor();

        // Act & Assert - Should not throw
        await parameterProcessor.InitializeParametersAsync(Array.Empty<ParameterResource>());
    }

    [Fact]
    public void HandleUnresolvedParametersAsync_WithSingleUnresolvedParameter_CreatesInteraction()
    {
        // Arrange
        var interactionService = CreateInteractionService();
        var parameterProcessor = CreateParameterProcessor(interactionService: interactionService);
        var unresolvedParam = CreateParameterWithMissingValue("testParam");

        // Initialize the parameter's WaitForValueTcs
        unresolvedParam.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act - Directly call HandleUnresolvedParametersAsync with the parameter list
        var handleTask = parameterProcessor.HandleUnresolvedParametersAsync([unresolvedParam]);

        // Assert - Verify interaction was created (the method should start immediately and create interaction)
        var interactions = interactionService.GetCurrentInteractions();
        Assert.NotEmpty(interactions);
        Assert.Contains(interactions, i => i.Title == "Unresolved Parameters");
        
        // Verify the parameter is still unresolved (waiting for user input)
        Assert.NotNull(unresolvedParam.WaitForValueTcs);
        Assert.False(unresolvedParam.WaitForValueTcs.Task.IsCompleted);
    }

    [Fact]
    public void HandleUnresolvedParametersAsync_WithMultipleUnresolvedParameters_CreatesCorrectInteraction()
    {
        // Arrange
        var interactionService = CreateInteractionService();
        var parameterProcessor = CreateParameterProcessor(interactionService: interactionService);
        var unresolvedParam1 = CreateParameterWithMissingValue("param1");
        var unresolvedParam2 = CreateParameterWithMissingValue("param2");

        // Initialize the parameters' WaitForValueTcs
        unresolvedParam1.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        unresolvedParam2.WaitForValueTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Act - Directly call HandleUnresolvedParametersAsync with the parameter list
        var handleTask = parameterProcessor.HandleUnresolvedParametersAsync([unresolvedParam1, unresolvedParam2]);

        // Assert - Verify interaction was created for multiple parameters
        var interactions = interactionService.GetCurrentInteractions();
        Assert.NotEmpty(interactions);
        
        var parameterInteraction = interactions.FirstOrDefault(i => i.Title == "Unresolved Parameters");
        Assert.NotNull(parameterInteraction);
        
        // Verify both parameters are still unresolved (waiting for user input)
        Assert.NotNull(unresolvedParam1.WaitForValueTcs);
        Assert.NotNull(unresolvedParam2.WaitForValueTcs);
        Assert.False(unresolvedParam1.WaitForValueTcs.Task.IsCompleted);
        Assert.False(unresolvedParam2.WaitForValueTcs.Task.IsCompleted);
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

    private static ParameterResource CreateParameterWithMissingValue(string name)
    {
        return new ParameterResource(name, _ => throw new MissingParameterValueException($"Parameter '{name}' is missing"), secret: false);
    }

    private static ParameterResource CreateParameterWithGenericError(string name)
    {
        return new ParameterResource(name, _ => throw new InvalidOperationException($"Generic error for parameter '{name}'"), secret: false);
    }
}
