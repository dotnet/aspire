// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.ApplicationModel;

internal static class CommandsConfigurationExtensions
{
    // Picked to provide a balance between enough time to start slow resources vs providing a timely timeout.
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(20);

    internal const string StartCommandName = "resource-start";
    internal const string StopCommandName = "resource-stop";
    internal const string RestartCommandName = "resource-restart";

    internal static void AddLifeCycleCommands(this IResource resource)
    {
        if (resource.TryGetLastAnnotation<ExcludeLifecycleCommandsAnnotation>(out _))
        {
            return;
        }

        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: StartCommandName,
            displayName: "Start",
            executeCommand: async context =>
            {
                return await RunLifeCycleCommandAsync(
                    async (serviceProvider, ct) =>
                    {
                        var applicationExecutor = serviceProvider.GetRequiredService<ApplicationExecutor>();
                        await applicationExecutor.StartResourceAsync(context.ResourceName, ct).ConfigureAwait(false);
                    },
                    context.ServiceProvider,
                    context.ResourceName,
                    IsRunning,
                    s_timeout,
                    context.CancellationToken).ConfigureAwait(false);
            },
            updateState: context =>
            {
                if (IsStarting(context.ResourceSnapshot.State?.Text) || IsWaiting(context.ResourceSnapshot.State?.Text))
                {
                    return ResourceCommandState.Disabled;
                }
                else if (IsStartable(context.ResourceSnapshot.State?.Text))
                {
                    return ResourceCommandState.Enabled;
                }
                else
                {
                    return ResourceCommandState.Hidden;
                }
            },
            displayDescription: null,
            parameter: null,
            confirmationMessage: null,
            iconName: "Play",
            iconVariant: IconVariant.Filled,
            isHighlighted: true));

        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: StopCommandName,
            displayName: "Stop",
            executeCommand: async context =>
            {
                return await RunLifeCycleCommandAsync(
                    async (serviceProvider, ct) =>
                    {
                        var applicationExecutor = serviceProvider.GetRequiredService<ApplicationExecutor>();
                        await applicationExecutor.StopResourceAsync(context.ResourceName, ct).ConfigureAwait(false);
                    },
                    context.ServiceProvider,
                    context.ResourceName,
                    IsStopped,
                    s_timeout,
                    context.CancellationToken).ConfigureAwait(false);
            },
            updateState: context =>
            {
                if (IsStopping(context.ResourceSnapshot.State?.Text))
                {
                    return ResourceCommandState.Disabled;
                }
                else if (!IsStartable(context.ResourceSnapshot.State?.Text) && !IsStarting(context.ResourceSnapshot.State?.Text) && !IsWaiting(context.ResourceSnapshot.State?.Text) && context.ResourceSnapshot.State is not null)
                {
                    return ResourceCommandState.Enabled;
                }
                else
                {
                    return ResourceCommandState.Hidden;
                }
            },
            displayDescription: null,
            parameter: null,
            confirmationMessage: null,
            iconName: "Stop",
            iconVariant: IconVariant.Filled,
            isHighlighted: true));

        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: RestartCommandName,
            displayName: "Restart",
            executeCommand: async context =>
            {
                return await RunLifeCycleCommandAsync(
                    async (serviceProvider, ct) =>
                    {
                        var applicationExecutor = serviceProvider.GetRequiredService<ApplicationExecutor>();
                        await applicationExecutor.StopResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                        await applicationExecutor.StartResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                    },
                    context.ServiceProvider,
                    context.ResourceName,
                    IsRunning,
                    s_timeout,
                    context.CancellationToken).ConfigureAwait(false);
            },
            updateState: context =>
            {
                if (IsWaiting(context.ResourceSnapshot.State?.Text) || IsStarting(context.ResourceSnapshot.State?.Text) || IsStopping(context.ResourceSnapshot.State?.Text) || IsStartable(context.ResourceSnapshot.State?.Text) || context.ResourceSnapshot.State is null)
                {
                    return ResourceCommandState.Disabled;
                }
                else
                {
                    return ResourceCommandState.Enabled;
                }
            },
            displayDescription: null,
            parameter: null,
            confirmationMessage: null,
            iconName: "ArrowCounterclockwise",
            iconVariant: IconVariant.Regular,
            isHighlighted: false));

        static bool IsStopped(string? state) => state is "Exited" or "Finished";
        static bool IsStartable(string? state) => IsStopped(state) || state is "FailedToStart";
        static bool IsStopping(string? state) => state is "Stopping";
        static bool IsStarting(string? state) => state is "Starting";
        static bool IsRunning(string? state) => state is "Running";
        static bool IsWaiting(string? state) => state is "Waiting" or "RuntimeUnhealthy";
    }

    internal static async Task<ExecuteCommandResult> RunLifeCycleCommandAsync(
        Func<IServiceProvider, CancellationToken, Task> action,
        IServiceProvider serviceProvider,
        string resourceName,
        Func<string, bool> isExpectedStateFunc,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        var resourceNotificationService = serviceProvider.GetRequiredService<ResourceNotificationService>();

        try
        {
            await action(serviceProvider, cts.Token).ConfigureAwait(false);

            await WaitForResourceAsync(resourceNotificationService, resourceName, isExpectedStateFunc, cts.Token).ConfigureAwait(false);

            cts.Token.ThrowIfCancellationRequested();

            return CommandResults.Success();
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            return new ExecuteCommandResult
            {
                Success = false,
                ErrorMessage = $"Timeout while waiting for '{resourceName}'."
            };
        }

        // This is different from ResourceNotificationService.WaitForResourceAsync implementation because it looks for resourceId instead of name.
        static async Task<string> WaitForResourceAsync(ResourceNotificationService resourceNotificationService, string resourceId, Func<string, bool> isExpectedStateFunc, CancellationToken cancellationToken = default)
        {
            await foreach (var resourceEvent in resourceNotificationService.WatchAsync(cancellationToken).ConfigureAwait(false))
            {
                if (string.Equals(resourceId, resourceEvent.ResourceId, StringComparisons.ResourceName)
                    && resourceEvent.Snapshot.State?.Text is { Length: > 0 } statusText
                    && isExpectedStateFunc(statusText))
                {
                    return statusText;
                }
            }

            throw new OperationCanceledException($"The operation was cancelled before the resource reached the target states.");
        }
    }
}
