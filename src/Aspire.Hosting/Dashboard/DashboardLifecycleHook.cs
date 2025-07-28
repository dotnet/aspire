// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Devcontainers.Codespaces;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                                             ILoggerFactory loggerFactory,
                                             DcpNameGenerator nameGenerator,
                                             IHostApplicationLifetime hostApplicationLifetime,
                                             IDistributedApplicationEventing eventing,
                                             CodespacesUrlRewriter codespaceUrlRewriter
                                             ) : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    // Internal for testing
    internal const string OtlpGrpcEndpointName = "otlp-grpc";
    internal const string OtlpHttpEndpointName = "otlp-http";

    private static readonly HashSet<string> s_suppressAutomaticConfigurationCopy = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        KnownConfigNames.DashboardCorsAllowedOrigins // Set on the dashboard's Dashboard:Otlp:Cors type
    };

    private Task? _dashboardLogsTask;
    private CancellationTokenSource? _dashboardLogsCts;

    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        Debug.Assert(executionContext.IsRunMode, "Dashboard resource should only be added in run mode");

        if (appModel.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is { } dashboardResource)
        {
            ConfigureAspireDashboardResource(dashboardResource);

            // Make the dashboard first in the list so it starts as fast as possible.
            appModel.Resources.Remove(dashboardResource);
            appModel.Resources.Insert(0, dashboardResource);
        }
        else
        {
            AddDashboardResource(appModel);
        }

        // Stop watching logs from the dashboard when the app host is stopping. Part of the app host shutdown is tearing down the dashboard service.
        // Dashboard services are killed while the dashboard is using them and will cause the dashboard to report an error accessing data.
        // By stopping here we prevent the app host from printing errors from the dashboard caused by shutdown.
        _dashboardLogsCts = CancellationTokenSource.CreateLinkedTokenSource(hostApplicationLifetime.ApplicationStopping);

        _dashboardLogsTask = WatchDashboardLogsAsync(_dashboardLogsCts.Token);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        // Stop listening to logs if the lifecycle hook is disposed without the app being shutdown.
        _dashboardLogsCts?.Cancel();

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

        nameGenerator.EnsureDcpInstancesPopulated(dashboardResource);

        ConfigureAspireDashboardResource(dashboardResource);
        // Make the dashboard first in the list so it starts as fast as possible.
        model.Resources.Insert(0, dashboardResource);
    }

    private void ConfigureAspireDashboardResource(IResource dashboardResource)
    {
        // The dashboard resource can be visible during development. We don't want people to be able to stop the dashboard from inside the dashboard.
        // Exclude the lifecycle commands from the dashboard resource so they're not accidently clicked during development.
        dashboardResource.Annotations.Add(new ExcludeLifecycleCommandsAnnotation());

        // Remove endpoint annotations because we are directly configuring
        // the dashboard app.
        var endpointAnnotations = dashboardResource.Annotations.OfType<EndpointAnnotation>().ToList();
        foreach (var endpointAnnotation in endpointAnnotations)
        {
            dashboardResource.Annotations.Remove(endpointAnnotation);
        }

        // Add the dashboard endpoints as non-proxied
        var options = dashboardOptions.Value;

        var dashboardUrls = options.DashboardUrl;
        var otlpGrpcEndpointUrl = options.OtlpGrpcEndpointUrl;
        var otlpHttpEndpointUrl = options.OtlpHttpEndpointUrl;

        eventing.Subscribe<ResourceReadyEvent>(dashboardResource, (context, resource) =>
        {
            var browserToken = options.DashboardToken;

            if (!StringUtils.TryGetUriFromDelimitedString(dashboardUrls, ";", out var firstDashboardUrl))
            {
                return Task.CompletedTask;
            }

            var dashboardUrl = codespaceUrlRewriter.RewriteUrl(firstDashboardUrl.ToString());

            distributedApplicationLogger.LogInformation("Now listening on: {DashboardUrl}", dashboardUrl.TrimEnd('/'));

            if (!string.IsNullOrEmpty(browserToken))
            {
                LoggingHelpers.WriteDashboardUrl(distributedApplicationLogger, dashboardUrl, browserToken, isContainer: false);
            }

            return Task.CompletedTask;
        });

        foreach (var d in dashboardUrls?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [])
        {
            var address = BindingAddress.Parse(d);

            dashboardResource.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, uriScheme: address.Scheme, port: address.Port, isProxied: true)
            {
                TargetHost = address.Host
            });
        }

        if (otlpGrpcEndpointUrl != null)
        {
            var address = BindingAddress.Parse(otlpGrpcEndpointUrl);
            dashboardResource.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, name: OtlpGrpcEndpointName, uriScheme: address.Scheme, port: address.Port, isProxied: true, transport: "http2")
            {
                TargetHost = address.Host
            });
        }

        if (otlpHttpEndpointUrl != null)
        {
            var address = BindingAddress.Parse(otlpHttpEndpointUrl);
            dashboardResource.Annotations.Add(new EndpointAnnotation(ProtocolType.Tcp, name: OtlpHttpEndpointName, uriScheme: address.Scheme, port: address.Port, isProxied: true)
            {
                TargetHost = address.Host
            });
        }

        var showDashboardResources = configuration.GetBool(KnownConfigNames.ShowDashboardResources, KnownConfigNames.Legacy.ShowDashboardResources);
        var hideDashboard = !(showDashboardResources ?? false);

        var snapshot = new CustomResourceSnapshot
        {
            Properties = [],
            ResourceType = dashboardResource switch
            {
                ExecutableResource => KnownResourceTypes.Executable,
                ProjectResource => KnownResourceTypes.Project,
                ContainerResource => KnownResourceTypes.Container,
                _ => dashboardResource.GetType().Name
            },
            IsHidden = hideDashboard
        };

        dashboardResource.Annotations.Add(new ResourceSnapshotAnnotation(snapshot));

        dashboardResource.Annotations.Add(new EnvironmentCallbackAnnotation(ConfigureEnvironmentVariables));
    }

    internal async Task ConfigureEnvironmentVariables(EnvironmentCallbackContext context)
    {
        // Automatically copy all configuration that starts with ASPIRE_DASHBOARD to the dashboard.
        // Do this first so there is no chance to overwrite any explicit configuration.
        // Some values are skipped because they're mapped to the Dashboard option type.
        foreach (var (name, value) in configuration.AsEnumerable())
        {
            if (name.StartsWith("ASPIRE_DASHBOARD_", StringComparison.OrdinalIgnoreCase) &&
                value != null &&
                !s_suppressAutomaticConfigurationCopy.Contains(name))
            {
                context.EnvironmentVariables[name] = value;
            }
        }

        var options = dashboardOptions.Value;

        // Options should have been validated these should not be null

        Debug.Assert(options.DashboardUrl is not null, "DashboardUrl should not be null");
        Debug.Assert(options.OtlpGrpcEndpointUrl is not null || options.OtlpHttpEndpointUrl is not null, "OtlpGrpcEndpointUrl and OtlpHttpEndpointUrl should not both be null");

        var environment = options.AspNetCoreEnvironment;
        var browserToken = options.DashboardToken;
        var otlpApiKey = options.OtlpApiKey;

        var resourceServiceUrl = await dashboardEndpointProvider.GetResourceServiceUriAsync(context.CancellationToken).ConfigureAwait(false);

        context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = environment;
        context.EnvironmentVariables[DashboardConfigNames.ResourceServiceUrlName.EnvVarName] = resourceServiceUrl;

        PopulateDashboardUrls(context);

        if (options.OtlpHttpEndpointUrl != null)
        {
            // Use explicitly defined allowed origins if configured.
            var allowedOrigins = configuration.GetString(KnownConfigNames.DashboardCorsAllowedOrigins, KnownConfigNames.Legacy.DashboardCorsAllowedOrigins);

            // If allowed origins are not configured then calculate allowed origins from endpoints.
            if (string.IsNullOrEmpty(allowedOrigins))
            {
                var model = context.ExecutionContext.ServiceProvider.GetRequiredService<DistributedApplicationModel>();
                allowedOrigins = GetAllowedOriginsFromResourceEndpoints(model);
            }

            if (!string.IsNullOrEmpty(allowedOrigins))
            {
                context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpCorsAllowedOriginsKeyName.EnvVarName] = allowedOrigins;
                context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpCorsAllowedHeadersKeyName.EnvVarName] = "*";
            }
        }

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

        // Details for contacting AspireServer in an IDE debug session.
        if (configuration["DEBUG_SESSION_PORT"] is { Length: > 0 } sessionPort)
        {
            // DEBUG_SESSION_PORT env var is in the format localhost:port.
            // We assume the address is localhost and only want the port value.
            if (sessionPort.Split(':') is { Length: 2 } parts &&
                int.TryParse(parts[1], CultureInfo.InvariantCulture, out var port))
            {
                context.EnvironmentVariables[DashboardConfigNames.DebugSessionPortName.EnvVarName] = port;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected DEBUG_SESSION_PORT value. Expected localhost:port, got '{sessionPort}'.");
            }
        }
        if (configuration["DEBUG_SESSION_TOKEN"] is { Length: > 0 } sessionToken)
        {
            context.EnvironmentVariables[DashboardConfigNames.DebugSessionTokenName.EnvVarName] = sessionToken;
        }
        if (configuration["DEBUG_SESSION_SERVER_CERTIFICATE"] is { Length: > 0 } sessionCertificate)
        {
            context.EnvironmentVariables[DashboardConfigNames.DebugSessionServerCertificateName.EnvVarName] = sessionCertificate;
        }
        if (options.TelemetryOptOut is { } optOutValue)
        {
            context.EnvironmentVariables[DashboardConfigNames.DebugSessionTelemetryOptOutName.EnvVarName] = optOutValue;
        }

    }

    private static void PopulateDashboardUrls(EnvironmentCallbackContext context)
    {
        var dashboardResource = (IResourceWithEndpoints)context.Resource;

        // We want to resolve the "target" URL for the dashboard to listen on.
        static ReferenceExpression GetTargetUrlExpression(EndpointReference e) =>
            ReferenceExpression.Create($"{e.Property(EndpointProperty.Scheme)}://{e.EndpointAnnotation.TargetHost}:{e.Property(EndpointProperty.TargetPort)}");

        var otlpGrpc = dashboardResource.GetEndpoint(OtlpGrpcEndpointName);
        if (otlpGrpc.Exists)
        {
            context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpGrpcUrlName.EnvVarName] = GetTargetUrlExpression(otlpGrpc);
        }

        var otlpHttp = dashboardResource.GetEndpoint(OtlpHttpEndpointName);
        if (otlpHttp.Exists)
        {
            context.EnvironmentVariables[DashboardConfigNames.DashboardOtlpHttpUrlName.EnvVarName] = GetTargetUrlExpression(otlpHttp);
        }

        var aspnetCoreUrls = new ReferenceExpressionBuilder();
        var first = true;

        // Turn http and https endpoints into a single ASPNETCORE_URLS environment variable.
        foreach (var e in dashboardResource.GetEndpoints().Where(e => e.EndpointName is "http" or "https"))
        {
            if (!first)
            {
                aspnetCoreUrls.AppendLiteral(";");
            }

            aspnetCoreUrls.Append($"{e.Property(EndpointProperty.Scheme)}://{e.EndpointAnnotation.TargetHost}:{e.Property(EndpointProperty.TargetPort)}");
            first = false;
        }

        if (!aspnetCoreUrls.IsEmpty)
        {
            // Combine into a single expression
            context.EnvironmentVariables[DashboardConfigNames.DashboardFrontendUrlName.EnvVarName] = aspnetCoreUrls.Build();
        }
    }

    private static string? GetAllowedOriginsFromResourceEndpoints(DistributedApplicationModel model)
    {
        var allResourceEndpoints = model.Resources
            .Where(r => !string.Equals(r.Name, KnownResourceNames.AspireDashboard, StringComparisons.ResourceName))
            .SelectMany(r => r.Annotations)
            .OfType<EndpointAnnotation>()
            .ToList();

        var corsOrigins = new HashSet<string>(StringComparers.UrlHost);
        foreach (var endpoint in allResourceEndpoints)
        {
            if (endpoint.UriScheme is "http" or "https")
            {
                // Prefer allocated endpoint over EndpointAnnotation.Port.
                var origin = endpoint.AllocatedEndpoint?.UriString;
                var targetOrigin = (endpoint.TargetPort != null)
                    ? $"{endpoint.UriScheme}://localhost:{endpoint.TargetPort}"
                    : null;

                if (origin != null)
                {
                    corsOrigins.Add(origin);
                }
                if (targetOrigin != null)
                {
                    corsOrigins.Add(targetOrigin);
                }
            }
        }

        if (corsOrigins.Count > 0)
        {
            return string.Join(',', corsOrigins);
        }

        return null;
    }

    private async Task WatchDashboardLogsAsync(CancellationToken cancellationToken)
    {
        var loggerCache = new ConcurrentDictionary<string, ILogger>(StringComparer.Ordinal);
        var defaultDashboardLogger = loggerFactory.CreateLogger("Aspire.Hosting.Dashboard");

        var dashboardResourceTasks = new Dictionary<string, Task>();

        try
        {
            await foreach (var notification in resourceNotificationService.WatchAsync(cancellationToken).ConfigureAwait(false))
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
                        var content = logLine.Content;
                        if (TimestampParser.TryParseConsoleTimestamp(content, out var result))
                        {
                            content = result.Value.ModifiedText;
                        }

                        logMessage = JsonSerializer.Deserialize(content, DashboardLogMessageContext.Default.DashboardLogMessage);
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

        if (logger.IsEnabled(logMessage.LogLevel))
        {
            // TODO: We should log the state as well.
            logger.Log(
                logMessage.LogLevel,
                logMessage.EventId,
                logMessage.Message,
                null,
                (s, _) => (logMessage.Exception is { } e) ? s + Environment.NewLine + e : s);
        }
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
    public string? Exception { get; set; }
    public JsonObject? State { get; set; }
}

[JsonSerializable(typeof(DashboardLogMessage))]
internal sealed partial class DashboardLogMessageContext : JsonSerializerContext
{

}
