// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Orchestrator;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.ApplicationModel;

internal static class CommandsConfigurationExtensions
{
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
                var orchestrator = context.ServiceProvider.GetRequiredService<ApplicationOrchestrator>();

                await orchestrator.StartResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                var state = context.ResourceSnapshot.State?.Text;
                if (IsStarting(state) || IsRuntimeUnhealthy(state))
                {
                    return ResourceCommandState.Disabled;
                }
                else if (IsStopped(state) || IsWaiting(state))
                {
                    return ResourceCommandState.Enabled;
                }
                else
                {
                    return ResourceCommandState.Hidden;
                }
            },
            displayDescription: "Start resource",
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
                var orchestrator = context.ServiceProvider.GetRequiredService<ApplicationOrchestrator>();

                await orchestrator.StopResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                var state = context.ResourceSnapshot.State?.Text;
                if (IsStopping(state))
                {
                    return ResourceCommandState.Disabled;
                }
                else if (!IsStopped(state) && !IsStarting(state) && !IsWaiting(state) && !IsRuntimeUnhealthy(state) && context.ResourceSnapshot.State is not null)
                {
                    return ResourceCommandState.Enabled;
                }
                else
                {
                    return ResourceCommandState.Hidden;
                }
            },
            displayDescription: "Stop resource",
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
                var orchestrator = context.ServiceProvider.GetRequiredService<ApplicationOrchestrator>();

                await orchestrator.StopResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                await orchestrator.StartResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                var state = context.ResourceSnapshot.State?.Text;
                if (IsStarting(state) || IsStopping(state) || IsStopped(state) || IsWaiting(state) || IsRuntimeUnhealthy(state) || context.ResourceSnapshot.State is null)
                {
                    return ResourceCommandState.Disabled;
                }
                else
                {
                    return ResourceCommandState.Enabled;
                }
            },
            displayDescription: "Restart resource",
            parameter: null,
            confirmationMessage: null,
            iconName: "ArrowCounterclockwise",
            iconVariant: IconVariant.Regular,
            isHighlighted: false));

        static bool IsStopped(string? state) => state is "Exited" or "Finished" or "FailedToStart";
        static bool IsStopping(string? state) => state is "Stopping";
        static bool IsStarting(string? state) => state is "Starting";
        static bool IsWaiting(string? state) => state is "Waiting";
        static bool IsRuntimeUnhealthy(string? state) => state is "RuntimeUnhealthy";
    }
}
