// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.DotNet;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Dcp;

/// <summary>
/// Helper class for creating CLI-owned dashboards via DCP.
/// Shared between DotNetAppHostProject and GuestAppHostProject.
/// </summary>
internal sealed class DcpDashboardHelper
{
    private readonly IDcpClient _dcpClient;
    private readonly ILogger _logger;

    public DcpDashboardHelper(IDcpClient dcpClient, ILogger logger)
    {
        _dcpClient = dcpClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a CLI-owned dashboard via DCP and configures environment variables for the AppHost.
    /// </summary>
    /// <param name="appHostInfo">AppHost information including dashboard path.</param>
    /// <param name="appHostProjectPath">Path to the AppHost project file.</param>
    /// <param name="dcpSession">The DCP session with kubeconfig path.</param>
    /// <param name="environmentVariables">Environment variables dictionary to populate for the AppHost.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if dashboard was created successfully, false otherwise.</returns>
    public async Task<bool> CreateCliOwnedDashboardAsync(
        AppHostInfo appHostInfo,
        string appHostProjectPath,
        DcpSession dcpSession,
        IDictionary<string, string> environmentVariables,
        CancellationToken cancellationToken)
    {
        // Read launchSettings.json from AppHost project to get dashboard configuration
        var launchSettings = LaunchSettingsReader.ReadLaunchSettings(appHostProjectPath);
        var launchProfile = LaunchSettingsReader.GetEffectiveLaunchProfile(launchSettings);

        if (launchProfile == null)
        {
            _logger.LogDebug("Could not find launch profile in launchSettings.json. Dashboard will be launched by AppHost.");
            return false;
        }

        if (string.IsNullOrEmpty(appHostInfo.DashboardPath))
        {
            _logger.LogDebug("Dashboard path not available. Dashboard will be launched by AppHost.");
            return false;
        }

        var profileEnv = launchProfile.EnvironmentVariables;

        // Get configuration from launchSettings
        var resourceServiceUrl = profileEnv.GetValueOrDefault("ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL");
        var otlpGrpcUrl = profileEnv.GetValueOrDefault("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL");
        var otlpHttpUrl = profileEnv.GetValueOrDefault("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL");
        var mcpUrl = profileEnv.GetValueOrDefault("ASPIRE_DASHBOARD_MCP_ENDPOINT_URL");
        var aspnetEnvironment = profileEnv.GetValueOrDefault("ASPNETCORE_ENVIRONMENT") ?? "Production";

        if (string.IsNullOrEmpty(resourceServiceUrl))
        {
            _logger.LogDebug("ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL not found in launchSettings. Dashboard will be launched by AppHost.");
            return false;
        }

        _logger.LogDebug("Dashboard will connect to resource service at {ResourceServiceUrl}", resourceServiceUrl);

        // Generate tokens/keys that will be shared between CLI, AppHost, and Dashboard
        var browserToken = Aspire.Hosting.TokenGenerator.GenerateToken();
        var resourceServiceApiKey = Aspire.Hosting.TokenGenerator.GenerateToken();
        var otlpApiKey = Aspire.Hosting.TokenGenerator.GenerateToken();
        var mcpApiKey = Aspire.Hosting.TokenGenerator.GenerateToken();

        // Dashboard frontend URL - use applicationUrl from launchSettings or default
        var dashboardFrontendUrl = launchProfile.ApplicationUrl ?? "http://localhost:18888";
        // Take just the first URL if there are multiple
        if (dashboardFrontendUrl.Contains(';'))
        {
            dashboardFrontendUrl = dashboardFrontendUrl.Split(';')[0];
        }

        _logger.LogDebug("Connecting to DCP at {KubeconfigPath}", dcpSession.KubeconfigPath);
        await _dcpClient.ConnectAsync(dcpSession.KubeconfigPath, cancellationToken);

        // Parse the dashboard URL to get scheme and desired port
        var dashboardUri = new Uri(dashboardFrontendUrl);
        var scheme = dashboardUri.Scheme;
        var desiredPort = dashboardUri.Port;

        // Create Service with Localhost mode (proxy) - DCP binds to the port and proxies to the dashboard
        var serviceName = "aspire-dashboard-http";
        var serviceSpec = new DcpServiceSpec(
            Name: serviceName,
            Port: desiredPort,
            Address: "localhost",
            Protocol: "TCP",
            AddressAllocationMode: "Localhost");

        _logger.LogDebug("Creating dashboard service {ServiceName} with port {Port} (Localhost/proxy)", serviceName, desiredPort);
        await _dcpClient.CreateServiceAsync(serviceSpec, cancellationToken);

        // Use portForServing expression - DCP will resolve this to the actual port the dashboard should bind to
        var portExpression = $$$"""{{- portForServing "{{{serviceName}}}" -}}""";
        var aspnetCoreUrls = $"{scheme}://localhost:{portExpression}";

        // Build dashboard environment variables
        var dashboardEnv = new Dictionary<string, string>
        {
            // Core URLs - use portForServing expression so DCP resolves the port at runtime
            ["ASPNETCORE_URLS"] = aspnetCoreUrls,
            ["ASPNETCORE_ENVIRONMENT"] = aspnetEnvironment,
            ["ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL"] = resourceServiceUrl,

            // Frontend auth
            ["DASHBOARD__FRONTEND__AUTHMODE"] = "BrowserToken",
            ["DASHBOARD__FRONTEND__BROWSERTOKEN"] = browserToken,

            // Resource service client auth
            ["DASHBOARD__RESOURCESERVICECLIENT__AUTHMODE"] = "ApiKey",
            ["DASHBOARD__RESOURCESERVICECLIENT__APIKEY"] = resourceServiceApiKey,

            // Enable debug logging for Aspire components to see DashboardClient connection/retry logs
            ["Logging__LogLevel__Aspire"] = "Debug"
        };

        // Add OTLP endpoints if configured
        // The Dashboard needs OTLP endpoints to receive telemetry from resources
        if (!string.IsNullOrEmpty(otlpGrpcUrl))
        {
            dashboardEnv["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = otlpGrpcUrl;
            dashboardEnv["DASHBOARD__OTLP__AUTHMODE"] = "ApiKey";
            dashboardEnv["DASHBOARD__OTLP__PRIMARYAPIKEY"] = otlpApiKey;
        }

        if (!string.IsNullOrEmpty(otlpHttpUrl))
        {
            dashboardEnv["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"] = otlpHttpUrl;
        }

        // MCP configuration: Always configure auth to avoid "Endpoint is unsecured" warning
        // When no explicit MCP URL is configured, the dashboard will serve MCP on the frontend endpoint
        // We can't fall back to the dashboard frontend URL here because DCP is proxying on that port
        dashboardEnv["DASHBOARD__MCP__AUTHMODE"] = "ApiKey";
        dashboardEnv["DASHBOARD__MCP__PRIMARYAPIKEY"] = mcpApiKey;
        dashboardEnv["DASHBOARD__MCP__USECLIMCP"] = "true";
        if (!string.IsNullOrEmpty(mcpUrl))
        {
            dashboardEnv["ASPIRE_DASHBOARD_MCP_ENDPOINT_URL"] = mcpUrl;
            _logger.LogDebug("MCP endpoint configured at {McpUrl}", mcpUrl);
        }
        else
        {
            _logger.LogDebug("MCP auth configured (no explicit endpoint, will use frontend)");
        }

        // Log all dashboard environment variables for debugging
        _logger.LogDebug("Dashboard environment variables:");
        foreach (var kvp in dashboardEnv)
        {
            // Don't log sensitive values
            var value = kvp.Key.Contains("KEY", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Contains("TOKEN", StringComparison.OrdinalIgnoreCase)
                ? "[REDACTED]"
                : kvp.Value;
            _logger.LogDebug("  {Key}={Value}", kvp.Key, value);
        }

        _logger.LogDebug("Creating dashboard executable in DCP");

        // Create ServiceProducerAnnotation JSON to link the Executable to the Service
        // This tells DCP that this executable produces the service, enabling portForServing resolution
        var serviceProducerAnnotation = JsonSerializer.Serialize(
            new[] { new DcpServiceProducerAnnotation { ServiceName = serviceName, Address = "localhost" } },
            DcpJsonContext.Default.DcpServiceProducerAnnotationArray);

        var dashboardSpec = new DcpExecutableSpec(
            Name: "aspire-dashboard",
            ExecutablePath: "dotnet",
            WorkingDirectory: Path.GetDirectoryName(appHostInfo.DashboardPath),
            Args: [appHostInfo.DashboardPath],
            Env: dashboardEnv,
            Annotations: new Dictionary<string, string>
            {
                ["service-producer"] = serviceProducerAnnotation
            });

        await _dcpClient.CreateExecutableAsync(dashboardSpec, cancellationToken);

        // Wait for the DCP Service to be Ready - this ensures the proxy is fully established
        // Without this, requests to the proxy port may hang intermittently
        _logger.LogDebug("Waiting for dashboard service to be ready...");
        await foreach (var serviceUpdate in _dcpClient.WatchServiceAsync(serviceName, cancellationToken))
        {
            _logger.LogDebug("Dashboard service state: {State}", serviceUpdate.State);
            if (string.Equals(serviceUpdate.State, "Ready", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Dashboard service is ready");
                break;
            }
            if (string.Equals(serviceUpdate.State, "Failed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(serviceUpdate.State, "FailedToStart", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Dashboard service failed to start");
                return false;
            }
        }

        // The effective URL for users is the proxy port (what DCP is listening on)
        var effectiveDashboardUrl = $"{scheme}://localhost:{desiredPort}";

        // Configure AppHost with CLI dashboard info so backchannel returns correct URL
        environmentVariables["ASPIRE_CLI_DASHBOARD_MODE"] = "true";
        environmentVariables["ASPIRE_CLI_DASHBOARD_URL"] = effectiveDashboardUrl;
        environmentVariables["ASPIRE_CLI_DASHBOARD_TOKEN"] = browserToken;

        // Copy essential env vars from launchSettings since we use --no-launch-profile
        environmentVariables["ASPNETCORE_ENVIRONMENT"] = aspnetEnvironment;
        environmentVariables["DOTNET_ENVIRONMENT"] = aspnetEnvironment;

        // Set ASPNETCORE_URLS from applicationUrl - AppHost needs this for Kestrel binding
        // Important: Also include the resource service endpoint URL so the AppHost binds to it
        var urls = new List<string>();
        if (!string.IsNullOrEmpty(launchProfile.ApplicationUrl))
        {
            urls.Add(launchProfile.ApplicationUrl);
        }
        if (!string.IsNullOrEmpty(resourceServiceUrl))
        {
            urls.Add(resourceServiceUrl);
        }
        if (urls.Count > 0)
        {
            environmentVariables["ASPNETCORE_URLS"] = string.Join(";", urls);
            _logger.LogDebug("AppHost ASPNETCORE_URLS set to: {Urls}", environmentVariables["ASPNETCORE_URLS"]);
        }

        if (!string.IsNullOrEmpty(resourceServiceUrl))
        {
            // Set both the new and legacy variable names:
            // - ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL: Used by the dashboard to connect to the resource service
            // - DOTNET_RESOURCE_SERVICE_ENDPOINT_URL: Used by DashboardServiceHost in AppHost to know what port to listen on
            environmentVariables["ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL"] = resourceServiceUrl;
            environmentVariables["DOTNET_RESOURCE_SERVICE_ENDPOINT_URL"] = resourceServiceUrl;
        }
        if (!string.IsNullOrEmpty(otlpGrpcUrl))
        {
            environmentVariables["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = otlpGrpcUrl;
        }
        if (!string.IsNullOrEmpty(otlpHttpUrl))
        {
            environmentVariables["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"] = otlpHttpUrl;
        }
        // Only set MCP endpoint URL if explicitly configured (can't use frontend URL due to DCP proxy)
        if (!string.IsNullOrEmpty(mcpUrl))
        {
            environmentVariables["ASPIRE_DASHBOARD_MCP_ENDPOINT_URL"] = mcpUrl;
        }

        // Configure AppHost with same API keys so the dashboard can authenticate with the resource service.
        // The AppHost's DistributedApplicationBuilder looks for ASPIRE_DASHBOARD_RESOURCESERVICE_APIKEY first
        // (see KnownConfigNames.DashboardResourceServiceClientApiKey), and if not found generates a new one.
        // We must set this exact variable so the AppHost uses our shared key instead of generating a new one.
        environmentVariables["ASPIRE_DASHBOARD_RESOURCESERVICE_APIKEY"] = resourceServiceApiKey;
        _logger.LogDebug("Set ASPIRE_DASHBOARD_RESOURCESERVICE_APIKEY for AppHost");

        if (!string.IsNullOrEmpty(otlpGrpcUrl))
        {
            environmentVariables["AppHost__OtlpApiKey"] = otlpApiKey;
        }

        // Always set MCP API key (MCP auth is always configured to avoid "unsecured" warning)
        environmentVariables["AppHost__McpApiKey"] = mcpApiKey;

        // Log all AppHost environment variables being set
        _logger.LogDebug("AppHost environment variables:");
        foreach (var kvp in environmentVariables)
        {
            var value = kvp.Key.Contains("KEY", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Contains("TOKEN", StringComparison.OrdinalIgnoreCase)
                ? "[REDACTED]"
                : kvp.Value;
            _logger.LogDebug("  {Key}={Value}", kvp.Key, value);
        }

        _logger.LogDebug("CLI-owned dashboard configured at {DashboardUrl}", effectiveDashboardUrl);
        return true;
    }

    /// <summary>
    /// Streams dashboard logs when debug mode is enabled.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task StreamDashboardLogsAsync(CancellationToken cancellationToken)
    {
        const string dashboardName = "aspire-dashboard";

        try
        {
            // Stream stdout
            var stdoutStream = await _dcpClient.GetLogStreamAsync(dashboardName, "stdout", follow: true, cancellationToken);
            _ = Task.Run(() => StreamLogsAsync(stdoutStream, "[Dashboard]", cancellationToken), cancellationToken);

            // Stream stderr
            var stderrStream = await _dcpClient.GetLogStreamAsync(dashboardName, "stderr", follow: true, cancellationToken);
            _ = Task.Run(() => StreamLogsAsync(stderrStream, "[Dashboard:err]", cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to start dashboard log streaming");
        }
    }

    private async Task StreamLogsAsync(Stream stream, string prefix, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(stream);
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    break;
                }
                _logger.LogDebug("{Prefix} {Line}", prefix, line);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading dashboard log stream for {Prefix}", prefix);
        }
    }
}
