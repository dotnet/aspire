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
        // Note: We don't wait for the Service to be ready here because in Localhost (proxy) mode,
        // the Service won't become Ready until the backend (dashboard) connects to it.
        var portExpression = $$$"""{{- portForServing "{{{serviceName}}}" -}}""";
        var aspnetCoreUrls = $"{scheme}://localhost:{portExpression}";

        // Build dashboard environment variables
        var dashboardEnv = new Dictionary<string, string>
        {
            // Core URLs - use portForServing expression so DCP resolves the port at runtime
            ["ASPNETCORE_URLS"] = aspnetCoreUrls,
            ["ASPNETCORE_ENVIRONMENT"] = aspnetEnvironment,
            ["DOTNET_RESOURCE_SERVICE_ENDPOINT_URL"] = resourceServiceUrl,

            // Frontend auth
            ["DASHBOARD__FRONTEND__AUTHMODE"] = "BrowserToken",
            ["DASHBOARD__FRONTEND__BROWSERTOKEN"] = browserToken,

            // Resource service client auth
            ["DASHBOARD__RESOURCESERVICECLIENT__AUTHMODE"] = "ApiKey",
            ["DASHBOARD__RESOURCESERVICECLIENT__APIKEY"] = resourceServiceApiKey,

            // Logging
            ["LOGGING__CONSOLE__FORMATTERNAME"] = "json"
        };

        // Add OTLP endpoints if configured
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

        // Add MCP endpoint if configured
        if (!string.IsNullOrEmpty(mcpUrl))
        {
            dashboardEnv["ASPIRE_DASHBOARD_MCP_ENDPOINT_URL"] = mcpUrl;
            dashboardEnv["DASHBOARD__MCP__AUTHMODE"] = "ApiKey";
            dashboardEnv["DASHBOARD__MCP__PRIMARYAPIKEY"] = mcpApiKey;
            dashboardEnv["DASHBOARD__MCP__USECLIMCP"] = "true";
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

        // Wait for dashboard to start running
        await foreach (var execInfo in _dcpClient.WatchExecutableAsync("aspire-dashboard", cancellationToken))
        {
            _logger.LogDebug("Dashboard state: {State}", execInfo.State);
            if (execInfo.State == "Running")
            {
                _logger.LogDebug("Dashboard is running with PID {Pid}", execInfo.Pid);
                break;
            }
            else if (execInfo.State is "FailedToStart" or "Terminated" or "Finished")
            {
                _logger.LogWarning("Dashboard failed to start with state {State}", execInfo.State);
                return false;
            }
        }

        // Now that the dashboard is running, wait for Service to become Ready
        // In Localhost (proxy) mode, the Service becomes Ready once the backend connects
        _logger.LogDebug("Waiting for service {ServiceName} to be ready", serviceName);
        int effectivePort = desiredPort;
        await foreach (var svc in _dcpClient.WatchServiceAsync(serviceName, cancellationToken))
        {
            _logger.LogDebug("Service state: {State}, EffectivePort: {Port}", svc.State, svc.EffectivePort);
            if (svc.State == "Ready")
            {
                effectivePort = svc.EffectivePort ?? desiredPort;
                break;
            }
        }

        // The effective URL for users is the proxy port (what DCP is listening on)
        var effectiveDashboardUrl = $"{scheme}://localhost:{effectivePort}";

        // Configure AppHost with CLI dashboard info so backchannel returns correct URL
        environmentVariables["ASPIRE_CLI_DASHBOARD_MODE"] = "true";
        environmentVariables["ASPIRE_CLI_DASHBOARD_URL"] = effectiveDashboardUrl;
        environmentVariables["ASPIRE_CLI_DASHBOARD_TOKEN"] = browserToken;

        // Copy essential env vars from launchSettings since we use --no-launch-profile
        environmentVariables["ASPNETCORE_ENVIRONMENT"] = aspnetEnvironment;
        environmentVariables["DOTNET_ENVIRONMENT"] = aspnetEnvironment;

        // Set ASPNETCORE_URLS from applicationUrl - AppHost needs this for Kestrel binding
        if (!string.IsNullOrEmpty(launchProfile.ApplicationUrl))
        {
            environmentVariables["ASPNETCORE_URLS"] = launchProfile.ApplicationUrl;
        }

        if (!string.IsNullOrEmpty(resourceServiceUrl))
        {
            environmentVariables["ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL"] = resourceServiceUrl;
        }
        if (!string.IsNullOrEmpty(otlpGrpcUrl))
        {
            environmentVariables["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = otlpGrpcUrl;
        }
        if (!string.IsNullOrEmpty(otlpHttpUrl))
        {
            environmentVariables["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"] = otlpHttpUrl;
        }
        if (!string.IsNullOrEmpty(mcpUrl))
        {
            environmentVariables["ASPIRE_DASHBOARD_MCP_ENDPOINT_URL"] = mcpUrl;
        }

        // Configure AppHost with same API keys so it can authenticate with dashboard
        environmentVariables["AppHost:ResourceService:AuthMode"] = "ApiKey";
        environmentVariables["AppHost:ResourceService:ApiKey"] = resourceServiceApiKey;

        if (!string.IsNullOrEmpty(otlpGrpcUrl))
        {
            environmentVariables["AppHost:OtlpApiKey"] = otlpApiKey;
        }

        if (!string.IsNullOrEmpty(mcpUrl))
        {
            environmentVariables["AppHost:McpApiKey"] = mcpApiKey;
        }

        _logger.LogDebug("CLI-owned dashboard configured at {DashboardUrl}", effectiveDashboardUrl);
        return true;
    }
}
