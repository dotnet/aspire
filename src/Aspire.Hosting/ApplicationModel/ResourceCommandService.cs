// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A service to execute resource commands.
/// </summary>
public class ResourceCommandService : IDisposable
{
    private readonly ConcurrentDictionary<string, IResource> _resources;
    private readonly CancellationTokenSource _cts;
    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly IServiceProvider _serviceProvider;

    internal ResourceCommandService(ResourceNotificationService resourceNotificationService, ResourceLoggerService resourceLoggerService, IServiceProvider serviceProvider)
    {
        _resourceLoggerService = resourceLoggerService;
        _serviceProvider = serviceProvider;
        _cts = new();
        _resources = new ConcurrentDictionary<string, IResource>(StringComparers.ResourceName);

        var cancellationToken = _cts.Token;

        Task.Run(async () =>
        {
            await foreach (var @event in resourceNotificationService.WatchAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                _resources[@event.ResourceId] = @event.Resource;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceId"></param>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ExecuteCommandResult> ExecuteCommandAsync(string resourceId, string command, CancellationToken cancellationToken = default)
    {
        if (!_resources.TryGetValue(resourceId, out var resource))
        {
            return new ExecuteCommandResult { Success = false, ErrorMessage = $"Resource '{resourceId}' not found." };
        }

        return await ExecuteCommandCoreAsync(resourceId, resource, command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ExecuteCommandResult> ExecuteCommandAsync(IResource resource, string command, CancellationToken cancellationToken = default)
    {
        var names = resource.GetResolvedResourceNames();
        // Single resource for IResource. Return its result directly.
        if (names.Length == 1)
        {
            return await ExecuteCommandCoreAsync(names[0], resource, command, cancellationToken).ConfigureAwait(false);
        }

        // Run commands for multiple resources in parallel.
        var tasks = new List<Task<ExecuteCommandResult>>();
        foreach (var name in names)
        {
            tasks.Add(ExecuteCommandCoreAsync(name, resource, command, cancellationToken));
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

    void IDisposable.Dispose()
    {
        _cts.Cancel();
    }

    internal async Task<ExecuteCommandResult> ExecuteCommandCoreAsync(string resourceId, IResource resource, string type, CancellationToken cancellationToken)
    {
        var logger = _resourceLoggerService.GetLogger(resourceId);

        logger.LogInformation("Executing command '{Type}'.", type);

        var annotation = resource.Annotations.OfType<ResourceCommandAnnotation>().SingleOrDefault(a => a.Name == type);
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
                    logger.LogInformation("Successfully executed command '{Type}'.", type);
                    return result;
                }
                else
                {
                    logger.LogInformation("Failure executed command '{Type}'. Error message: {ErrorMessage}", type, result.ErrorMessage);
                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing command '{Type}'.", type);
                return new ExecuteCommandResult { Success = false, ErrorMessage = "Unhandled exception thrown." };
            }
        }

        logger.LogInformation("Command '{Type}' not available.", type);
        return new ExecuteCommandResult { Success = false, ErrorMessage = "Command type not available." };
    }
}
