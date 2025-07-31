// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Exec;

/// <summary>
/// A service to execute container exec commands.
/// </summary>
public class ContainerExecService
{
    private readonly ResourceNotificationService _resourceNotificationService;
    private readonly ResourceLoggerService _resourceLoggerService;

    private readonly IDcpExecutor _dcpExecutor;
    private readonly DcpNameGenerator _dcpNameGenerator;

    internal ContainerExecService(
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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="ExecuteCommandResult" /> indicates command success or failure.</returns>
    public async IAsyncEnumerable<ContainerExecCommandOutput> ExecuteCommandAsync(string resourceId, string commandName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_resourceNotificationService.TryGetCurrentState(resourceId, out var resourceEvent))
        {
            yield return new()
            {
                Text = $"Resource '{resourceId}' not found.",
                IsErrorMessage = true
            };
            yield break;
        }

        if (resourceEvent.Resource is not ContainerResource containerResource)
        {
            yield return new()
            {
                Text = $"Resource '{resourceId}' is not a container resource.",
                IsErrorMessage = true
            };
            yield break;
        }

        var outputLogs = ExecuteCommandCoreAsync(resourceEvent.ResourceId, containerResource, commandName, cancellationToken);
        await foreach (var output in outputLogs.WithCancellation(cancellationToken))
        {
            yield return output;
        }
    }

    /// <summary>
    /// Execute a command for the specified resource.
    /// </summary>
    /// <param name="resource">The resource. If the resource has multiple instances, such as replicas, then the command will be executed for each instance.</param>
    /// <param name="commandName">The command name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="ExecuteCommandResult" /> indicates command success or failure.</returns>
    public IAsyncEnumerable<ContainerExecCommandOutput> ExecuteCommandAsync(ContainerResource resource, string commandName, CancellationToken cancellationToken = default)
    {
        var names = resource.GetResolvedResourceNames();
        return ExecuteCommandCoreAsync(names[0], resource, commandName, cancellationToken);
    }

    internal async IAsyncEnumerable<ContainerExecCommandOutput> ExecuteCommandCoreAsync(
        string resourceId,
        ContainerResource resource,
        string commandName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var logger = _resourceLoggerService.GetLogger(resourceId);
        logger.LogInformation("Executing command '{CommandName}'.", commandName);

        var annotation = resource.Annotations.OfType<ResourceContainerExecCommandAnnotation>().SingleOrDefault(a => a.Name == commandName);
        if (annotation is null)
        {
            logger.LogInformation("Command '{CommandName}' not available.", commandName);
            yield return new()
            {
                Text = $"Command '{commandName}' not available for resource '{resourceId}'.",
                IsErrorMessage = true,
            };

            yield break;
        }

        var containerExecResource = new ContainerExecutableResource(annotation.Name, resource, annotation.Command, annotation.WorkingDirectory);
        _dcpNameGenerator.EnsureDcpInstancesPopulated(containerExecResource);
        var dcpResourceName = containerExecResource.GetResolvedResourceName();

        // in the background wait for the exec resource to reach terminal state. Once done we can complete logging
        _ = Task.Run(async () =>
        {
            await _resourceNotificationService.WaitForResourceAsync(containerExecResource.Name, targetStates: KnownResourceStates.TerminalStates, cancellationToken).ConfigureAwait(false);

            // hack: https://github.com/dotnet/aspire/issues/10245
            // workarounds the race-condition between streaming all logs from the resource, and resource completion
            await Task.Delay(1000, CancellationToken.None).ConfigureAwait(false);

            _resourceLoggerService.Complete(dcpResourceName); // complete stops the `WatchAsync` async-foreach below
        }, cancellationToken);

        // start the ephemeral resource execution
        var runResourceTask = _dcpExecutor.RunEphemeralResourceAsync(containerExecResource, cancellationToken);

        // subscribe to the logs of the resource
        // log stream will be stopped by the background "completion awaiting" task
        await foreach (var logs in _resourceLoggerService.WatchAsync(dcpResourceName).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (var log in logs)
            {
                yield return new ContainerExecCommandOutput
                {
                    Text = log.Content,
                    IsErrorMessage = log.IsErrorMessage,
                    LineNumber = log.LineNumber
                };
            }
        }

        // wait for the resource to complete execution
        await runResourceTask.ConfigureAwait(false);
    }
}
