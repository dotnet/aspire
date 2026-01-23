// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Terminals;

/// <summary>
/// Extension methods for adding terminal support to resources.
/// </summary>
public static class TerminalResourceBuilderExtensions
{
    /// <summary>
    /// Attaches an interactive terminal to the resource.
    /// </summary>
    /// <typeparam name="T">The type of resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method allocates a terminal for the resource that can be accessed via a WebSocket connection.
    /// A URL annotation is added to the resource pointing to an xterm.js test page.
    /// </para>
    /// <para>
    /// The terminal is backed by a Unix domain socket that the resource process can connect to
    /// for bidirectional I/O.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<T> WithTerminal<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithTerminal
    {
        // Add annotation to indicate this resource wants a terminal
        builder.WithAnnotation(new TerminalAnnotation());

        // Register the event subscriber to allocate terminals before start
        builder.ApplicationBuilder.Services.TryAddEventingSubscriber<TerminalEventSubscriber>();

        return builder;
    }
}

/// <summary>
/// Annotation indicating that a resource should have an interactive terminal attached.
/// </summary>
internal sealed class TerminalAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Gets or sets the allocated terminal information once the terminal is created.
    /// </summary>
    public AllocatedTerminal? AllocatedTerminal { get; set; }
}

/// <summary>
/// Event subscriber that allocates terminals for resources with the <see cref="TerminalAnnotation"/>.
/// </summary>
internal sealed class TerminalEventSubscriber : IDistributedApplicationEventingSubscriber
{
    private readonly TerminalHost _terminalHost;
    private readonly ResourceNotificationService _notificationService;

    public TerminalEventSubscriber(TerminalHost terminalHost, ResourceNotificationService notificationService)
    {
        _terminalHost = terminalHost;
        _notificationService = notificationService;
    }

    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (executionContext.IsRunMode)
        {
            eventing.Subscribe<BeforeStartEvent>(OnBeforeStartAsync);
        }

        return Task.CompletedTask;
    }

    private async Task OnBeforeStartAsync(BeforeStartEvent evt, CancellationToken cancellationToken)
    {
        foreach (var resource in evt.Model.Resources)
        {
            if (resource.TryGetLastAnnotation<TerminalAnnotation>(out var terminalAnnotation))
            {
                // Allocate terminal
                var allocation = await _terminalHost.AllocateTerminalAsync(cancellationToken).ConfigureAwait(false);
                terminalAnnotation.AllocatedTerminal = allocation;

                // Add URL annotation for the test page
                resource.Annotations.Add(new ResourceUrlAnnotation
                {
                    Url = allocation.TestPageUrl,
                    DisplayText = "Terminal",
                    DisplayLocation = UrlDisplayLocation.SummaryAndDetails
                });

                // Add environment variable with socket path for the resource to connect
                if (resource is IResourceWithEnvironment resourceWithEnv)
                {
                    resourceWithEnv.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
                    {
                        // Socket path for connecting to the terminal
                        context.EnvironmentVariables["ASPIRE_TERMINAL_SOCKET"] = allocation.SocketPath;

                        // Terminal type - xterm-256color provides full color and Unicode support
                        context.EnvironmentVariables["TERM"] = "xterm-256color";

                        // Ensure UTF-8 encoding for proper Unicode box-drawing characters
                        context.EnvironmentVariables["LANG"] = "en_US.UTF-8";
                        context.EnvironmentVariables["LC_ALL"] = "en_US.UTF-8";

                        // Force color output for tools that check for terminal capabilities
                        context.EnvironmentVariables["COLORTERM"] = "truecolor";
                    }));
                }

                // Publish update so the dashboard receives the terminal URL
                await _notificationService.PublishUpdateAsync(resource, s => s).ConfigureAwait(false);
            }
        }
    }
}
