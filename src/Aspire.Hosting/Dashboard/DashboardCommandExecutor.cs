// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Used to execute annotations in the dashboard.
/// Although commands are received by the dashboard host, it's important that they're executed
/// in the context of the app host service provider. That allows commands to access user registered services.
/// </summary>
internal sealed class DashboardCommandExecutor
{
    private readonly IServiceProvider _appHostServiceProvider;
    private readonly ResourceLoggerService _resourceLoggerService;

    public DashboardCommandExecutor(ResourceLoggerService resourceLoggerService, IServiceProvider appHostServiceProvider)
    {
        _resourceLoggerService = resourceLoggerService;
        _appHostServiceProvider = appHostServiceProvider;
    }

    public async Task<(ExecuteCommandResultType result, string? errorMessage)> ExecuteCommandAsync(string resourceId, IResource resource, string type, CancellationToken cancellationToken)
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
                    ServiceProvider = _appHostServiceProvider,
                    CancellationToken = cancellationToken
                };

                var result = await annotation.ExecuteCommand(context).ConfigureAwait(false);
                if (result.Success)
                {
                    logger.LogInformation("Successfully executed command '{Type}'.", type);
                    return (ExecuteCommandResultType.Success, null);
                }
                else
                {
                    logger.LogInformation("Failure executed command '{Type}'. Error message: {ErrorMessage}", type, result.ErrorMessage);
                    return (ExecuteCommandResultType.Failure, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing command '{Type}'.", type);
                return (ExecuteCommandResultType.Failure, "Unhandled exception thrown.");
            }
        }

        logger.LogInformation("Command '{Type}' not available.", type);
        return (ExecuteCommandResultType.Canceled, null);

    }
}

internal enum ExecuteCommandResultType
{
    Success,
    Failure,
    Canceled
}
