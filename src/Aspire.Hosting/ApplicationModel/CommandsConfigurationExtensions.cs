// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Properties;
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
            displayName: Commands.StartCommandDisplayName,
            getDisplayName: context => Commands.ResourceManager.GetString(nameof(Commands.StartCommandDisplayName), CultureInfo.GetCultureInfo(context.Locale))!,
            executeCommand: async context =>
            {
                var executor = context.ServiceProvider.GetRequiredService<ApplicationExecutor>();

                await executor.StartResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                if (IsStarting(context.ResourceSnapshot.State?.Text) || IsWaiting(context.ResourceSnapshot.State?.Text))
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
            displayDescription: Commands.StartCommandDisplayDescription,
            getDisplayDescription: context => Commands.ResourceManager.GetString(nameof(Commands.StartCommandDisplayDescription), CultureInfo.GetCultureInfo(context.Locale))!,
            parameter: null,
            confirmationMessage: null,
            getConfirmationMessage: null,
            iconName: "Play",
            iconVariant: IconVariant.Filled,
            isHighlighted: true));

        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: StopCommandName,
            displayName: Commands.StopCommandDisplayName,
            getDisplayName: context => Commands.ResourceManager.GetString(nameof(Commands.StopCommandDisplayName), CultureInfo.GetCultureInfo(context.Locale))!,
            executeCommand: async context =>
            {
                var executor = context.ServiceProvider.GetRequiredService<ApplicationExecutor>();

                await executor.StopResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                if (IsStopping(context.ResourceSnapshot.State?.Text))
                {
                    return ResourceCommandState.Disabled;
                }
                else if (!IsStopped(context.ResourceSnapshot.State?.Text) && !IsStarting(context.ResourceSnapshot.State?.Text) && !IsWaiting(context.ResourceSnapshot.State?.Text) && context.ResourceSnapshot.State is not null)
                {
                    return ResourceCommandState.Enabled;
                }
                else
                {
                    return ResourceCommandState.Hidden;
                }
            },
            displayDescription: Commands.StopCommandDisplayDescription,
            getDisplayDescription: context => Commands.ResourceManager.GetString(nameof(Commands.StopCommandDisplayDescription), CultureInfo.GetCultureInfo(context.Locale))!,
            parameter: null,
            confirmationMessage: null,
            getConfirmationMessage: null,
            iconName: "Stop",
            iconVariant: IconVariant.Filled,
            isHighlighted: true));

        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: RestartCommandName,
            displayName: Commands.RestartCommandDisplayName,
            getDisplayName: context => Commands.ResourceManager.GetString(nameof(Commands.RestartCommandDisplayName), CultureInfo.GetCultureInfo(context.Locale))!,
            executeCommand: async context =>
            {
                var executor = context.ServiceProvider.GetRequiredService<ApplicationExecutor>();

                await executor.StopResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                await executor.StartResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                return CommandResults.Success();
            },
            updateState: context =>
            {
                if (IsWaiting(context.ResourceSnapshot.State?.Text) || IsStarting(context.ResourceSnapshot.State?.Text) || IsStopping(context.ResourceSnapshot.State?.Text) || IsStopped(context.ResourceSnapshot.State?.Text) || context.ResourceSnapshot.State is null)
                {
                    return ResourceCommandState.Disabled;
                }
                else
                {
                    return ResourceCommandState.Enabled;
                }
            },
            displayDescription: Commands.RestartCommandDisplayDescription,
            getDisplayDescription: context => Commands.ResourceManager.GetString(nameof(Commands.RestartCommandDisplayDescription), CultureInfo.GetCultureInfo(context.Locale))!,
            parameter: null,
            confirmationMessage: null,
            getConfirmationMessage: null,
            iconName: "ArrowCounterclockwise",
            iconVariant: IconVariant.Regular,
            isHighlighted: false));

        static bool IsStopped(string? state) => state is "Exited" or "Finished" or "FailedToStart";
        static bool IsStopping(string? state) => state is "Stopping";
        static bool IsStarting(string? state) => state is "Starting";
        static bool IsWaiting(string? state) => state is "Waiting" or "RuntimeUnhealthy";
    }
}
