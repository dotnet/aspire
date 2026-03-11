// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Orchestrator;
using Aspire.Hosting.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            displayName: CommandStrings.StartName,
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
            displayDescription: CommandStrings.StartDescription,
            parameter: null,
            confirmationMessage: null,
            iconName: "Play",
            iconVariant: IconVariant.Filled,
            isHighlighted: true));

        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: KnownResourceCommands.StopCommand,
            displayName: CommandStrings.StopName,
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
            displayDescription: CommandStrings.StopDescription,
            parameter: null,
            confirmationMessage: null,
            iconName: "Stop",
            iconVariant: IconVariant.Filled,
            isHighlighted: true));

        // Use a more detailed description for .NET projects to help AI understand
        // that source code changes won't take effect until rebuilding the project.
        var restartDescription = resource is ProjectResource
            ? CommandStrings.RestartProjectDescription
            : CommandStrings.RestartDescription;

        resource.Annotations.Add(new ResourceCommandAnnotation(
            name: KnownResourceCommands.RestartCommand,
            displayName: CommandStrings.RestartName,
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
            displayDescription: restartDescription,
            parameter: null,
            confirmationMessage: null,
            iconName: "ArrowCounterclockwise",
            iconVariant: IconVariant.Regular,
            isHighlighted: false));

        if (resource is ProjectResource projectResource)
        {
            resource.Annotations.Add(new ResourceCommandAnnotation(
                name: KnownResourceCommands.RebuildCommand,
                displayName: CommandStrings.RebuildName,
                executeCommand: async context =>
                {
                    var orchestrator = context.ServiceProvider.GetRequiredService<ApplicationOrchestrator>();
                    var resourceNotificationService = context.ServiceProvider.GetRequiredService<ResourceNotificationService>();
                    var loggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
                    var model = context.ServiceProvider.GetRequiredService<DistributedApplicationModel>();

                    var rebuilderResource = model.Resources.OfType<ProjectRebuilderResource>().FirstOrDefault(r => r.Parent == projectResource);
                    if (rebuilderResource is null)
                    {
                        return new ExecuteCommandResult { Success = false, ErrorMessage = $"Rebuilder resource for '{projectResource.Name}' not found." };
                    }

                    var mainLogger = loggerService.GetLogger(projectResource);

                    // Stop the main resource first.
                    mainLogger.LogInformation("[build] Stopping resource for rebuild...");
                    await orchestrator.StopResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);

                    // Update main resource state to indicate building is in progress.
                    await resourceNotificationService.PublishUpdateAsync(projectResource, s => s with
                    {
                        State = new ResourceStateSnapshot("Building", KnownResourceStateStyles.Info)
                    }).ConfigureAwait(false);

                    // Start forwarding logs from the rebuilder to the main resource's console.
                    using var logCts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
                    var rebuilderName = rebuilderResource.Name;
                    var logForwardTask = ForwardLogsAsync(loggerService, rebuilderName, mainLogger, logCts.Token);

                    // Start the rebuilder resource (runs dotnet build).
                    mainLogger.LogInformation("[build] Building project...");
                    await orchestrator.StartResourceAsync(rebuilderName, context.CancellationToken).ConfigureAwait(false);

                    // Wait for the rebuilder to reach a terminal state.
                    int? exitCode = null;
                    await foreach (var evt in resourceNotificationService.WatchAsync(context.CancellationToken).ConfigureAwait(false))
                    {
                        if (evt.Resource == rebuilderResource &&
                            KnownResourceStates.TerminalStates.Contains(evt.Snapshot.State?.Text))
                        {
                            exitCode = evt.Snapshot.ExitCode;
                            break;
                        }
                    }

                    // Stop log forwarding.
                    await logCts.CancelAsync().ConfigureAwait(false);

                    try
                    {
                        await logForwardTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelling the log forwarder.
                    }

                    if (exitCode == 0)
                    {
                        mainLogger.LogInformation("[build] Build succeeded. Restarting resource...");
                        await orchestrator.StartResourceAsync(context.ResourceName, context.CancellationToken).ConfigureAwait(false);
                        return CommandResults.Success();
                    }
                    else
                    {
                        mainLogger.LogError("[build] Build failed with exit code {ExitCode}.", exitCode);
                        await resourceNotificationService.PublishUpdateAsync(projectResource, s => s with
                        {
                            State = new ResourceStateSnapshot("Build failed", KnownResourceStateStyles.Error)
                        }).ConfigureAwait(false);
                        return new ExecuteCommandResult { Success = false, ErrorMessage = $"Build failed with exit code {exitCode}." };
                    }
                },
                updateState: context =>
                {
                    var state = context.ResourceSnapshot.State?.Text;
                    if (IsStarting(state) || IsStopping(state) || IsStopped(state) || IsWaiting(state) || IsRuntimeUnhealthy(state) || HasNoState(state) || IsBuilding(state))
                    {
                        return ResourceCommandState.Disabled;
                    }
                    else
                    {
                        return ResourceCommandState.Enabled;
                    }
                },
                displayDescription: CommandStrings.RebuildDescription,
                parameter: null,
                confirmationMessage: null,
                iconName: "ArrowSync",
                iconVariant: IconVariant.Regular,
                isHighlighted: false));
        }

        // Treat "Unknown" as stopped so the command to start the resource is available when "Unknown".
        // There is a situation where a container can be stopped with this state: https://github.com/dotnet/aspire/issues/5977
        static bool IsStopped(string? state) => KnownResourceStates.TerminalStates.Contains(state) || state == KnownResourceStates.NotStarted || state == "Unknown";
        static bool IsStopping(string? state) => state == KnownResourceStates.Stopping;
        static bool IsStarting(string? state) => state == KnownResourceStates.Starting;
        static bool IsWaiting(string? state) => state == KnownResourceStates.Waiting;
        static bool IsRuntimeUnhealthy(string? state) => state == KnownResourceStates.RuntimeUnhealthy;
        static bool HasNoState(string? state) => string.IsNullOrEmpty(state);
        static bool IsBuilding(string? state) => state is "Building";
    }

    private static async Task ForwardLogsAsync(ResourceLoggerService loggerService, string sourceResourceName, ILogger targetLogger, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var batch in loggerService.WatchAsync(sourceResourceName).WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                foreach (var line in batch)
                {
                    if (line.IsErrorMessage)
                    {
                        targetLogger.LogWarning("[build] {Content}", line.Content);
                    }
                    else
                    {
                        targetLogger.LogInformation("[build] {Content}", line.Content);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected when the log forwarding is cancelled.
        }
    }
}
