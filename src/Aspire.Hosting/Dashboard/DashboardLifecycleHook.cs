// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal sealed class DashboardLifecycleHook(IConfiguration configuration,
                                             IOptions<DashboardOptions> dashboardOptions,
                                             ILogger<DistributedApplication> distributedApplicationLogger,
                                             IDashboardEndpointProvider dashboardEndpointProvider,
                                             DistributedApplicationExecutionContext executionContext,
                                             ResourceNotificationService resourceNotificationService,
                                             ResourceLoggerService resourceLoggerService,
                                             ILoggerFactory loggerFactory) : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    private readonly CancellationTokenSource _shutdownCts = new();
    private Task? _dashboardLogsTask;

    public Task BeforeStartAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        Debug.Assert(executionContext.IsRunMode, "Dashboard resource should only be added in run mode");

        if (model.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is { } dashboardResource)
        {
            ConfigureAspireDashboardResource(dashboardResource);

            // Make the dashboard first in the list so it starts as fast as possible.
            model.Resources.Remove(dashboardResource);
            model.Resources.Insert(0, dashboardResource);
        }
        else
        {
            AddDashboardResource(model);
        }

        _dashboardLogsTask = WatchDashboardLogsAsync(_shutdownCts.Token);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _shutdownCts.Cancel();

        if (_dashboardLogsTask is not null)
        {
            try
            {
                await _dashboardLogsTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when the application is shutting down.
            }
            catch (Exception ex)
            {
                distributedApplicationLogger.LogError(ex, "Unexpected error while watching dashboard logs.");
            }
        }
    }

    private void AddDashboardResource(DistributedApplicationModel model)
    {
        if (dashboardOptions.Value.DashboardPath is not { } dashboardPath)
        {
            throw new DistributedApplicationException("Dashboard path empty or file does not exist.");
        }

        var fullyQualifiedDashboardPath = Path.GetFullPath(dashboardPath);
        var dashboardWorkingDirectory = Path.GetDirectoryName(fullyQualifiedDashboardPath);

        ExecutableResource? dashboardResource = default;

        if (string.Equals(".dll", Path.GetExtension(fullyQualifiedDashboardPath), StringComparison.OrdinalIgnoreCase))
        {
            // The dashboard path is a DLL, so run it with `dotnet <dll>`
            dashboardResource = new ExecutableResource(KnownResourceNames.AspireDashboard, "dotnet", dashboardWorkingDirectory ?? "");

            dashboardResource.Annotations.Add(new CommandLineArgsCallbackAnnotation(args =>
            {
                args.Add(fullyQualifiedDashboardPath);
            }));
        }
        else
        {
            // Assume the dashboard path is directly executable
            dashboardResource = new ExecutableResource(KnownResourceNames.AspireDashboard, fullyQualifiedDashboardPath, dashboardWorkingDirectory ?? "");
        }

        ConfigureAspireDashboardResource(dashboardResource);

        // Make the dashboard first in the list so it starts as fast as possible.
        model.Resources.Insert(0, dashboardResource);
    }

    private void ConfigureAspireDashboardResource(IResource dashboardResource)
    {
        // Remove endpoint annotations because we are directly configuring
        // the dashboard app (it doesn't go through the proxy!).
        var endpointAnnotations = dashboardResource.Annotations.OfType<EndpointAnnotation>().ToList();
        foreach (var endpointAnnotation in endpointAnnotations)
        {
            dashboardResource.Annotations.Remove(endpointAnnotation);
        }

        var snapshot = new CustomResourceSnapshot()
        {
            Properties = [],
            ResourceType = dashboardResource switch
            {
                ExecutableResource => KnownResourceTypes.Executable,
                ProjectResource => KnownResourceTypes.Project,
                ContainerResource => KnownResourceTypes.Container,
                _ => dashboardResource.GetType().Name
            },
            State = configuration.GetBool("DOTNET_ASPIRE_SHOW_DASHBOARD_RESOURCES") is true ? null : KnownResourceStates.Hidden
        };

        dashboardResource.Annotations.Add(new ResourceSnapshotAnnotation(snapshot));

        dashboardResource.Annotations.Add(new EnvironmentCallbackAnnotation(async context =>
        {
            var options = dashboardOptions.Value;

            // Options should have been validated these should not be null

            Debug.Assert(options.DashboardUrl is not null, "DashboardUrl should not be null");
            Debug.Assert(options.OtlpEndpointUrl is not null, "OtlpEndpointUrl should not be null");

            var dashboardUrls = options.DashboardUrl;
            var otlpEndpointUrl = options.OtlpEndpointUrl;

            var environment = options.AspNetCoreEnvironment;
            var browserToken = options.DashboardToken;
            var otlpApiKey = options.OtlpApiKey;

            var resourceServiceUrl = await dashboardEndpointProvider.GetResourceServiceUriAsync(context.CancellationToken).ConfigureAwait(false);

            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = environment;
            context.EnvironmentVariables[DashboardConfigNames.DashboardFrontendUrlName.EnvVarName] = dashboardUrls;
            context.EnvironmentVariables[DashboardConfigNames.ResourceServiceUrlName.EnvVarName] = resourceServiceUrl;
            context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpUrlName.EnvVarName] = otlpEndpointUrl;

            // Configure frontend browser token
            if (!string.IsNullOrEmpty(browserToken))
            {
                context.EnvironmentVariables[DashboardConfigNames.DashboardFrontendAuthModeName.EnvVarName] = "BrowserToken";
                context.EnvironmentVariables[DashboardConfigNames.DashboardFrontendBrowserTokenName.EnvVarName] = browserToken;
            }
            else
            {
                context.EnvironmentVariables[DashboardConfigNames.DashboardFrontendAuthModeName.EnvVarName] = "Unsecured";
            }

            // Configure resource service API key
            if (string.Equals(configuration["AppHost:ResourceService:AuthMode"], nameof(ResourceServiceAuthMode.ApiKey), StringComparison.OrdinalIgnoreCase)
                && configuration["AppHost:ResourceService:ApiKey"] is { Length: > 0 } resourceServiceApiKey)
            {
                context.EnvironmentVariables[DashboardConfigNames.ResourceServiceClientAuthModeName.EnvVarName] = nameof(ResourceServiceAuthMode.ApiKey);
                context.EnvironmentVariables[DashboardConfigNames.ResourceServiceClientApiKeyName.EnvVarName] = resourceServiceApiKey;
            }
            else
            {
                context.EnvironmentVariables[DashboardConfigNames.ResourceServiceClientAuthModeName.EnvVarName] = nameof(ResourceServiceAuthMode.Unsecured);
            }

            // Configure OTLP API key
            if (!string.IsNullOrEmpty(otlpApiKey))
            {
                context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpAuthModeName.EnvVarName] = "ApiKey";
                context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpPrimaryApiKeyName.EnvVarName] = otlpApiKey;
            }
            else
            {
                context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpAuthModeName.EnvVarName] = "Unsecured";
            }

            // Change the dashboard formatter to use JSON so we can parse the logs and render them in the
            // via the ILogger.
            context.EnvironmentVariables["LOGGING__CONSOLE__FORMATTERNAME"] = "json";

            // We need to print out the url so that dotnet watch can launch the dashboard
            // technically this is too early, but it's late ne
            if (StringUtils.TryGetUriFromDelimitedString(dashboardUrls, ";", out var firstDashboardUrl))
            {
                distributedApplicationLogger.LogInformation("Now listening on: {DashboardUrl}", firstDashboardUrl.ToString().TrimEnd('/'));
            }

            if (!string.IsNullOrEmpty(browserToken))
            {
                LoggingHelpers.WriteDashboardUrl(distributedApplicationLogger, dashboardUrls, browserToken);
            }
        }));
    }

    private async Task WatchDashboardLogsAsync(CancellationToken cancellationToken)
    {
        var loggerCache = new ConcurrentDictionary<string, ILogger>(StringComparer.Ordinal);
        var defaultDashboardLogger = loggerFactory.CreateLogger("Aspire.Hosting.Dashboard");

        var dashboardResourceTasks = new Dictionary<string, Task>();

        try
        {
            await foreach (var notification in resourceNotificationService.WatchAsync(cancellationToken).ConfigureAwait(true)) // Setting ConfigureAwait(true)  to silence CA2007. Consider calling ConfigureAwait(false) instead.
            {
                // Track all dashboard resources and start watching their logs.
                // TODO: In the future when resources can restart, we should handle purging the taskCache.
                if (StringComparers.ResourceName.Equals(notification.Resource.Name, KnownResourceNames.AspireDashboard) && !dashboardResourceTasks.ContainsKey(notification.ResourceId))
                {
                    dashboardResourceTasks[notification.ResourceId] = WatchResourceLogsAsync(notification.ResourceId, loggerCache, defaultDashboardLogger, resourceLoggerService, loggerFactory, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the application is shutting down.
        }
        catch (Exception ex)
        {
            defaultDashboardLogger.LogError(ex, "Error reading dashboard logs.");
        }

        // The watch task should already be logging exceptions, so we don't need to log them here.
        await Task.WhenAll(dashboardResourceTasks.Values).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private static async Task WatchResourceLogsAsync(string dashboardResourceId,
                                                     ConcurrentDictionary<string, ILogger> loggerCache,
                                                     ILogger defaultDashboardLogger,
                                                     ResourceLoggerService resourceLoggerService,
                                                     ILoggerFactory loggerFactory,
                                                     CancellationToken cancellationToken)
    {
        try
        {
            // We turned on the JSON formatter for the logger, so we can log the log lines as JSON.
            await foreach (var batch in resourceLoggerService.WatchAsync(dashboardResourceId).WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                foreach (var logLine in batch)
                {
                    DashboardLogMessage? logMessage = null;

                    try
                    {
                        logMessage = JsonSerializer.Deserialize(logLine.Content, DashboardLogMessageContext.Default.DashboardLogMessage);
                    }
                    catch (JsonException)
                    {
                        if (defaultDashboardLogger.IsEnabled(LogLevel.Debug))
                        {
                            defaultDashboardLogger.LogDebug("Failed to parse dashboard log line as JSON: {LogLine}", logLine.Content);
                        }

                        // Failed to parse, it's not JSON, just log the content as is.
                        var level = logLine.IsErrorMessage ? LogLevel.Error : LogLevel.Information;
                        defaultDashboardLogger.Log(level, 0, logLine.Content, null, (s, _) => s);
                    }

                    if (logMessage is not null)
                    {
                        LogMessage(loggerFactory, loggerCache, logMessage);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the application is shutting down.
        }
        catch (Exception ex)
        {
            defaultDashboardLogger.LogError(ex, "Error reading dashboard logs.");
        }
        finally
        {
            if (defaultDashboardLogger.IsEnabled(LogLevel.Debug))
            {
                defaultDashboardLogger.LogDebug("Stopped reading dashboard logs.");
            }
        }
    }

    private static void LogMessage(ILoggerFactory loggerFactory, ConcurrentDictionary<string, ILogger> loggerCache, DashboardLogMessage logMessage)
    {
        var logger = loggerCache.GetOrAdd(logMessage.Category, static (string category, ILoggerFactory loggerFactory) =>
        {
            // Looks strange to see Aspire.Hosting.Dashboard.Aspire.Dashboard.Category,
            // so trim the prefix and append Aspire.Hosting.Why is this important?
            // Well there are logs emitting from categories that don't start with Aspire.Dashboard so we want to prefix all logs so that they can be controlled by config.
            var categoryTrimmed = category.StartsWith("Aspire.Dashboard.") ?
                category["Aspire.Dashboard.".Length..] : category;

            return loggerFactory.CreateLogger($"Aspire.Hosting.Dashboard.{categoryTrimmed}");
        },
        loggerFactory);

        // TODO: We should log the state as well.
        logger.Log(logMessage.LogLevel, logMessage.EventId, logMessage.Message, null, (s, _) => s);
    }
}

internal sealed class DashboardLogMessage
{
    public string Timestamp { get; set; } = string.Empty;
    public int EventId { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter<LogLevel>))]
    public LogLevel LogLevel { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public JsonObject? State { get; set; }
}

[JsonSerializable(typeof(DashboardLogMessage))]
internal sealed partial class DashboardLogMessageContext : JsonSerializerContext
{

}
