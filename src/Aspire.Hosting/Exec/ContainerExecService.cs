// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    internal ContainerExecService(ResourceNotificationService resourceNotificationService, ResourceLoggerService resourceLoggerService, IDcpExecutor dcpExecutor)
    {
        _resourceNotificationService = resourceNotificationService;
        _resourceLoggerService = resourceLoggerService;
        _dcpExecutor = dcpExecutor;
    }

    /// <summary>
    /// Execute a command for the specified resource.
    /// </summary>
    /// <param name="resource">The resource. If the resource has multiple instances, such as replicas, then the command will be executed for each instance.</param>
    /// <param name="commandName">The command name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="ExecuteCommandResult" /> indicates command success or failure.</returns>
    public async Task<bool> ExecuteCommandAsync(ContainerResource resource, string commandName, CancellationToken cancellationToken = default)
    {
        var names = resource.GetResolvedResourceNames();
        // Single resource for IResource. Return its result directly.
        if (names.Length == 1)
        {
            return await ExecuteCommandCoreAsync(names[0], resource, commandName, cancellationToken).ConfigureAwait(false);
        }

        throw new NotImplementedException();

        //// Run commands for multiple resources in parallel.
        //var tasks = new List<Task<bool>>();
        //foreach (var name in names)
        //{
        //    tasks.Add(ExecuteCommandCoreAsync(name, resource, commandName, cancellationToken));
        //}

        //// Check for failures.
        //var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        //var failures = new List<(string resourceId, ExecuteCommandResult result)>();
        //for (var i = 0; i < results.Length; i++)
        //{
        //    if (!results[i].Success)
        //    {
        //        failures.Add((names[i], results[i]));
        //    }
        //}

        //if (failures.Count == 0)
        //{
        //    return new ExecuteCommandResult { Success = true };
        //}
        //else
        //{
        //    // Aggregate error results together.
        //    var errorMessage = $"{failures.Count} command executions failed.";
        //    errorMessage += Environment.NewLine + string.Join(Environment.NewLine, failures.Select(f => $"Resource '{f.resourceId}' failed with error message: {f.result.ErrorMessage}"));

        //    return new ExecuteCommandResult
        //    {
        //        Success = false,
        //        ErrorMessage = errorMessage
        //    };
        //}
    }

    internal async Task<bool> ExecuteCommandCoreAsync(string resourceId, ContainerResource resource, string commandName, CancellationToken cancellationToken)
    {
        var logger = _resourceLoggerService.GetLogger(resourceId);
        logger.LogInformation("Executing command '{CommandName}'.", commandName);

        var annotation = resource.Annotations.OfType<ResourceContainerExecCommandAnnotation>().SingleOrDefault(a => a.Name == commandName);
        if (annotation is null)
        {
            logger.LogInformation("Command '{CommandName}' not available.", commandName);
            // return new ExecuteCommandResult { Success = false, ErrorMessage = $"Command '{commandName}' not available for resource '{resourceId}'." };
            return false;
        }

        try
        {
            var containerExecResource = new ContainerExecutableResource(annotation.Name, resource, annotation.Command, annotation.WorkingDirectory);

            await _dcpExecutor.RunEphemeralResourceAsync(containerExecResource, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command '{CommandName}'.", commandName);
            // return new ExecuteCommandResult { Success = false, ErrorMessage = "Unhandled exception thrown." };
            return false;
        }
    }
}
