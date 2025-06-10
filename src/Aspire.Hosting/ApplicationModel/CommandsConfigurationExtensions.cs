// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Orchestrator;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.ApplicationModel;

internal static class CommandsConfigurationExtensions
{
    internal static void AddLifeCycleCommands(this IResource resource)
    {
        if (resource.TryGetLastAnnotation<ExcludeLifecycleCommandsAnnotation>(out _))
        {
            return;
        }

        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: KnownResourceCommands.StartCommand,
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
                if (IsStarting(state) || IsRuntimeUnhealthy(state) || HasNoState(state))
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
            name: KnownResourceCommands.StopCommand,
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
                else if (!IsStopped(state) && !IsStarting(state) && !IsWaiting(state) && !IsRuntimeUnhealthy(state) && !HasNoState(state))
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
            name: KnownResourceCommands.RestartCommand,
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
                if (IsStarting(state) || IsStopping(state) || IsStopped(state) || IsWaiting(state) || IsRuntimeUnhealthy(state) || HasNoState(state))
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

        // Treat "Unknown" as stopped so the command to start the resource is available when "Unknown".
        // There is a situation where a container can be stopped with this state: https://github.com/dotnet/aspire/issues/5977
        static bool IsStopped(string? state) => KnownResourceStates.TerminalStates.Contains(state) || state == KnownResourceStates.NotStarted || state == "Unknown";
        static bool IsStopping(string? state) => state == KnownResourceStates.Stopping;
        static bool IsStarting(string? state) => state == KnownResourceStates.Starting;
        static bool IsWaiting(string? state) => state == KnownResourceStates.Waiting;
        static bool IsRuntimeUnhealthy(string? state) => state == KnownResourceStates.RuntimeUnhealthy;
        static bool HasNoState(string? state) => string.IsNullOrEmpty(state);
    }
}
