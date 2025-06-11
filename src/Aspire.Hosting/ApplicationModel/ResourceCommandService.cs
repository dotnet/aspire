// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A service to execute resource commands.
/// </summary>
public class ResourceCommandService
{
    private readonly ResourceNotificationService _resourceNotificationService;
    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly IServiceProvider _serviceProvider;

    internal ResourceCommandService(ResourceNotificationService resourceNotificationService, ResourceLoggerService resourceLoggerService, IServiceProvider serviceProvider)
    {
        _resourceNotificationService = resourceNotificationService;
        _resourceLoggerService = resourceLoggerService;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Execute a command for the specified resource.
    /// </summary>
    /// <param name="resourceId">The id of the resource.</param>
    /// <param name="commandName">The command name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="ExecuteCommandResult" /> indicates command success or failure.</returns>
    public async Task<ExecuteCommandResult> ExecuteCommandAsync(string resourceId, string commandName, CancellationToken cancellationToken = default)
    {
        if (!_resourceNotificationService.TryGetCurrentState(resourceId, out var resourceEvent))
        {
            return new ExecuteCommandResult { Success = false, ErrorMessage = $"Resource '{resourceId}' not found." };
        }

        return await ExecuteCommandCoreAsync(resourceEvent.ResourceId, resourceEvent.Resource, commandName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute a command for the specified resource.
    /// </summary>
    /// <param name="resource">The resource. If the resource has multiple instances, such as replicas, then the command will be executed for each instance.</param>
    /// <param name="commandName">The command name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="ExecuteCommandResult" /> indicates command success or failure.</returns>
    public async Task<ExecuteCommandResult> ExecuteCommandAsync(IResource resource, string commandName, CancellationToken cancellationToken = default)
    {
        var names = resource.GetResolvedResourceNames();
        // Single resource for IResource. Return its result directly.
        if (names.Length == 1)
        {
            return await ExecuteCommandCoreAsync(names[0], resource, commandName, cancellationToken).ConfigureAwait(false);
        }

        // Run commands for multiple resources in parallel.
        var tasks = new List<Task<ExecuteCommandResult>>();
        foreach (var name in names)
        {
            tasks.Add(ExecuteCommandCoreAsync(name, resource, commandName, cancellationToken));
        }

        // Check for failures.
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var failures = new List<(string resourceId, ExecuteCommandResult result)>();
        for (var i = 0; i < results.Length; i++)
        {
            if (!results[i].Success)
            {
                failures.Add((names[i], results[i]));
            }
        }

        if (failures.Count == 0)
        {
            return new ExecuteCommandResult { Success = true };
        }
        else
        {
            // Aggregate error results together.
            var errorMessage = $"{failures.Count} command executions failed.";
            errorMessage += Environment.NewLine + string.Join(Environment.NewLine, failures.Select(f => $"Resource '{f.resourceId}' failed with error message: {f.result.ErrorMessage}"));

            return new ExecuteCommandResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    internal async Task<ExecuteCommandResult> ExecuteCommandCoreAsync(string resourceId, IResource resource, string commandName, CancellationToken cancellationToken)
    {
        var logger = _resourceLoggerService.GetLogger(resourceId);

        logger.LogInformation("Executing command '{CommandName}'.", commandName);

        var annotation = resource.Annotations.OfType<ResourceCommandAnnotation>().SingleOrDefault(a => a.Name == commandName);
        if (annotation != null)
        {
            try
            {
                var context = new ExecuteCommandContext
                {
                    ResourceName = resourceId,
                    ServiceProvider = _serviceProvider,
                    CancellationToken = cancellationToken
                };

                var result = await annotation.ExecuteCommand(context).ConfigureAwait(false);
                if (result.Success)
                {
                    logger.LogInformation("Successfully executed command '{CommandName}'.", commandName);
                    return result;
                }
                else
                {
                    logger.LogInformation("Failure executing command '{CommandName}'. Error message: {ErrorMessage}", commandName, result.ErrorMessage);
                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing command '{CommandName}'.", commandName);
                return new ExecuteCommandResult { Success = false, ErrorMessage = "Unhandled exception thrown." };
            }
        }

        logger.LogInformation("Command '{CommandName}' not available.", commandName);
        return new ExecuteCommandResult { Success = false, ErrorMessage = $"Command '{commandName}' not available for resource '{resourceId}'." };
    }
}
