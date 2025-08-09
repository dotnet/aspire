// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Exec;

/// <summary>
/// A service to execute container exec commands.
/// </summary>
internal class ContainerExecService : IContainerExecService
{
    private readonly ResourceNotificationService _resourceNotificationService;
    private readonly ResourceLoggerService _resourceLoggerService;

    private readonly IDcpExecutor _dcpExecutor;
    private readonly DcpNameGenerator _dcpNameGenerator;

    public ContainerExecService(
        ResourceNotificationService resourceNotificationService,
        ResourceLoggerService resourceLoggerService,
        IDcpExecutor dcpExecutor,
        DcpNameGenerator dcpNameGenerator)
    {
        _resourceNotificationService = resourceNotificationService;
        _resourceLoggerService = resourceLoggerService;

        _dcpExecutor = dcpExecutor;
        _dcpNameGenerator = dcpNameGenerator;
    }

    /// <summary>
    /// Execute a command for the specified resource.
    /// </summary>
    /// <param name="resourceId">The specific id of the resource instance.</param>
    /// <param name="commandName">The command name.</param>
    /// <returns>The <see cref="ExecuteCommandResult" /> indicates command success or failure.</returns>
    public ExecCommandRun ExecuteCommand(string resourceId, string commandName)
    {
        if (!_resourceNotificationService.TryGetCurrentState(resourceId, out var resourceEvent))
        {
            return new()
            {
                ExecuteCommand = token => Task.FromResult(CommandResults.Failure($"Failed to get the resource {resourceId}"))
            };
        }

        var resource = resourceEvent.Resource;
        if (resource is not ContainerResource containerResource)
        {
            throw new ArgumentException("Resource is not a container resource.", nameof(resourceId));
        }

        return ExecuteCommand(containerResource, commandName);
    }

    public ExecCommandRun ExecuteCommand(ContainerResource containerResource, string commandName)
    {
        var annotation = containerResource.Annotations.OfType<ResourceExecCommandAnnotation>().SingleOrDefault(a => a.Name == commandName);
        if (annotation is null)
        {
            return new()
            {
                ExecuteCommand = token => Task.FromResult(CommandResults.Failure($"Failed to get the resource {containerResource.Name}"))
            };
        }

        return ExecuteCommandCore(containerResource, annotation.Name, annotation.Command, annotation.WorkingDirectory);
    }

    /// <summary>
    /// Executes a command for the specified resource.
    /// </summary>
    /// <param name="resource">The resource to execute a command in.</param>
    /// <param name="commandName"></param>
    /// <param name="command"></param>
    /// <param name="workingDirectory"></param>
    /// <returns></returns>
    private ExecCommandRun ExecuteCommandCore(
        ContainerResource resource,
        string commandName,
        string command,
        string? workingDirectory)
    {
        var resourceId = resource.GetResolvedResourceNames().First();

        var logger = _resourceLoggerService.GetLogger(resourceId);
        logger.LogInformation("Starting command '{Command}' on resource {ResourceId}", command, resourceId);

        var containerExecResource = new ContainerExecutableResource(commandName, resource, command, workingDirectory);
        _dcpNameGenerator.EnsureDcpInstancesPopulated(containerExecResource);
        var dcpResourceName = containerExecResource.GetResolvedResourceName();

        Func<CancellationToken, Task<ExecuteCommandResult>> commandResultTask = async (CancellationToken cancellationToken) =>
        {
            await _dcpExecutor.RunEphemeralResourceAsync(containerExecResource, cancellationToken).ConfigureAwait(false);
            await _resourceNotificationService.WaitForResourceAsync(containerExecResource.Name, targetStates: KnownResourceStates.TerminalStates, cancellationToken).ConfigureAwait(false);

            if (!_resourceNotificationService.TryGetCurrentState(dcpResourceName, out var resourceEvent))
            {
                return CommandResults.Failure("Failed to fetch command results.");
            }

            // resource completed execution, so we can complete the log stream
            _resourceLoggerService.Complete(dcpResourceName);

            var snapshot = resourceEvent.Snapshot;
            return snapshot.ExitCode is 0
                ? CommandResults.Success()
                : CommandResults.Failure($"Command failed with exit code {snapshot.ExitCode}. Final state: {resourceEvent.Snapshot.State?.Text}.");
        };

        return new ExecCommandRun
        {
            ExecuteCommand = commandResultTask,
            GetOutputStream = token => GetResourceLogsStreamAsync(dcpResourceName, token)
        };
    }

    private async IAsyncEnumerable<LogLine> GetResourceLogsStreamAsync(string dcpResourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IAsyncEnumerable<IReadOnlyList<LogLine>> source;
        if (_resourceNotificationService.TryGetCurrentState(dcpResourceName, out var resourceEvent)
            && resourceEvent.Snapshot.ExitCode is not null)
        {
            // If the resource is already in a terminal state, we can just return the logs that were already collected.
            source = _resourceLoggerService.GetAllAsync(dcpResourceName);
        }
        else
        {
            // resource is still running, so we can stream the logs as they come in.
            source = _resourceLoggerService.WatchAsync(dcpResourceName);
        }

        await foreach (var batch in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (var logLine in batch)
            {
                yield return logLine;
            }
        }
    }
}
