// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.ApplicationModel;

internal static class CommandsConfigurationExtensions
{
    internal const string StartType = "start";
    internal const string StopType = "stop";
    internal const string RestartType = "restart";

    internal static IResourceBuilder<T> WithLifeCycleCommands<T>(this IResourceBuilder<T> builder) where T : IResource
    {
        builder.WithCommand(
            type: StartType,
            displayName: "Start",
            executeCommand: async context =>
            {
                var executor = context.ServiceProvider.GetRequiredService<ApplicationExecutor>();

                await executor.StartResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                if (IsStarting(context.ResourceSnapshot.State?.Text))
                {
                    return ResourceCommandState.Disabled;
                }
                else if (IsStopped(context.ResourceSnapshot.State?.Text))
                {
                    return ResourceCommandState.Enabled;
                }
                else
                {
                    return ResourceCommandState.Hidden;
                }
            },
            iconName: "Play",
            isHighlighted: true);

        builder.WithCommand(
            type: StopType,
            displayName: "Stop",
            executeCommand: async context =>
            {
                var executor = context.ServiceProvider.GetRequiredService<ApplicationExecutor>();

                await executor.StopResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                if (IsWaiting(context.ResourceSnapshot.State?.Text) || IsStopping(context.ResourceSnapshot.State?.Text))
                {
                    return ResourceCommandState.Disabled;
                }
                else if (!IsStopped(context.ResourceSnapshot.State?.Text) && !IsStarting(context.ResourceSnapshot.State?.Text))
                {
                    return ResourceCommandState.Enabled;
                }
                else
                {
                    return ResourceCommandState.Hidden;
                }
            },
            iconName: "Stop",
            isHighlighted: true);

        builder.WithCommand(
            type: RestartType,
            displayName: "Restart",
            executeCommand: async context =>
            {
                var executor = context.ServiceProvider.GetRequiredService<ApplicationExecutor>();

                await executor.StopResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                await executor.StartResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                if (IsWaiting(context.ResourceSnapshot.State?.Text) || IsStarting(context.ResourceSnapshot.State?.Text) || IsStopping(context.ResourceSnapshot.State?.Text) || IsStopped(context.ResourceSnapshot.State?.Text))
                {
                    return ResourceCommandState.Disabled;
                }
                else
                {
                    return ResourceCommandState.Enabled;
                }
            },
            iconName: "ArrowCounterclockwise",
            isHighlighted: false);

        return builder;

        static bool IsStopped(string? state) => state is "Exited" or "Finished" or "FailedToStart";
        static bool IsStopping(string? state) => state is "Stopping";
        static bool IsStarting(string? state) => state is "Starting";
        static bool IsWaiting(string? state) => state is "Waiting";
    }
}
