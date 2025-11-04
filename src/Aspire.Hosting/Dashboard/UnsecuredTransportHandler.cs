// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

/// <summary>
/// Hosted service that handles unsecured transport warnings by showing an interactive modal to the user.
/// </summary>
internal sealed class UnsecuredTransportHandler : IHostedService, IDistributedApplicationEventingSubscriber
{
    private readonly UnsecuredTransportWarning _unsecuredTransportWarning;
    private readonly IInteractionService _interactionService;
    private readonly ILogger<UnsecuredTransportHandler> _logger;
    private readonly DistributedApplicationExecutionContext _executionContext;
    private readonly TaskCompletionSource<bool> _dashboardReadyTcs = new();
    private Task? _interactionTask;

    public UnsecuredTransportHandler(
        UnsecuredTransportWarning unsecuredTransportWarning,
        IInteractionService interactionService,
        ILogger<UnsecuredTransportHandler> logger,
        DistributedApplicationExecutionContext executionContext)
    {
        _unsecuredTransportWarning = unsecuredTransportWarning;
        _interactionService = interactionService;
        _logger = logger;
        _executionContext = executionContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start the interaction task asynchronously - don't block app startup
        _interactionTask = HandleUnsecuredTransportAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        // Only subscribe in run mode
        if (executionContext.IsRunMode)
        {
            // Subscribe to ResourceReadyEvent for the dashboard
            eventing.Subscribe<ResourceReadyEvent>(OnResourceReadyAsync);
            
            // Subscribe to BeforeResourceStartedEvent to block non-dashboard resources until modal is handled
            eventing.Subscribe<BeforeResourceStartedEvent>(OnBeforeResourceStartedAsync);
        }

        return Task.CompletedTask;
    }

    private Task OnResourceReadyAsync(ResourceReadyEvent @event, CancellationToken cancellationToken)
    {
        // Check if this is the dashboard resource
        if (StringComparers.ResourceName.Equals(@event.Resource.Name, KnownResourceNames.AspireDashboard))
        {
            // Signal that the dashboard is ready
            _dashboardReadyTcs.TrySetResult(true);
        }

        return Task.CompletedTask;
    }

    private async Task OnBeforeResourceStartedAsync(BeforeResourceStartedEvent @event, CancellationToken cancellationToken)
    {
        // Don't block the dashboard itself from starting
        if (StringComparers.ResourceName.Equals(@event.Resource.Name, KnownResourceNames.AspireDashboard))
        {
            return;
        }

        // Block all other resources until the interaction is complete
        if (_interactionTask is not null)
        {
            await _interactionTask.ConfigureAwait(false);
        }
    }

    private async Task HandleUnsecuredTransportAsync(CancellationToken cancellationToken)
    {
        // Only check in run mode, not in publish mode
        if (_executionContext.IsPublishMode)
        {
            return;
        }

        // If there are no warnings, nothing to do
        if (!_unsecuredTransportWarning.HasWarnings)
        {
            return;
        }

        // If the interaction service is not available (e.g., dashboard disabled), 
        // log warnings and exit the process
        if (!_interactionService.IsAvailable)
        {
            foreach (var warning in _unsecuredTransportWarning.Warnings)
            {
                _logger.LogError("Unsecured transport detected: {Warning}", warning);
            }
            _logger.LogError("The application is configured to use unsecured transport (HTTP) but the '{EnvVar}' environment variable is not set. Either enable HTTPS or set {EnvVar}=true to allow unsecured transport. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.", 
                KnownConfigNames.AllowUnsecuredTransport, KnownConfigNames.AllowUnsecuredTransport);
            
            // Exit the process with an error code
            Environment.Exit(1);
            return;
        }

        // Wait for the dashboard to be ready before showing the modal
        await _dashboardReadyTcs.Task.ConfigureAwait(false);

        // Show a blocking modal dialog to the user
        var title = "Unsecured Transport Detected";
        var message = "The application is configured to use unsecured transport (HTTP). This means that sensitive data may be transmitted without encryption.\n\n" +
                     "To resolve this issue, you can either:\n" +
                     "• Enable HTTPS in your launch profile settings\n" +
                     $"• Set the '{KnownConfigNames.AllowUnsecuredTransport}' environment variable to 'true' to allow unsecured transport\n\n" +
                     "For more information, visit: https://aka.ms/dotnet/aspire/allowunsecuredtransport\n\n" +
                     "Do you want to continue running with unsecured transport?";

        var options = new MessageBoxInteractionOptions
        {
            Intent = MessageIntent.Warning,
            ShowSecondaryButton = true,
            PrimaryButtonText = "Continue",
            SecondaryButtonText = "Quit",
            ShowDismiss = false,
            EnableMessageMarkdown = false
        };

        var result = await _interactionService.PromptConfirmationAsync(title, message, options, cancellationToken).ConfigureAwait(false);

        if (result.Canceled || !result.Data)
        {
            // User chose to quit or dismissed the dialog
            _logger.LogWarning("User declined to continue with unsecured transport. Exiting application.");
            Environment.Exit(0);
            return;
        }

        // User chose to continue
        _logger.LogWarning("User accepted running with unsecured transport.");
        _unsecuredTransportWarning.UserAcceptedRisk = true;

        // Show a notification at the top of the dashboard
        var notificationTitle = "Running with Unsecured Transport";
        var notificationMessage = "The application is using unsecured transport (HTTP). Sensitive data may be transmitted without encryption.";
        var notificationOptions = new NotificationInteractionOptions
        {
            Intent = MessageIntent.Warning,
            LinkText = "Learn more",
            LinkUrl = "https://aka.ms/dotnet/aspire/allowunsecuredtransport"
        };

        // Fire and forget - don't wait for the notification to be dismissed
        _ = _interactionService.PromptNotificationAsync(notificationTitle, notificationMessage, notificationOptions, CancellationToken.None);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
