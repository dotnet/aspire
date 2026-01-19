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

        // Generate deterministic tokens/keys based on session path
        // This ensures the same keys are used across apphost restarts (watch mode hot reload)
        // so the dashboard doesn't need to be recreated
        var browserToken = GenerateDeterministicToken(dcpSession.SessionDir, "browser");
        var resourceServiceApiKey = GenerateDeterministicToken(dcpSession.SessionDir, "resourceService");
        var otlpApiKey = GenerateDeterministicToken(dcpSession.SessionDir, "otlp");
        var mcpApiKey = GenerateDeterministicToken(dcpSession.SessionDir, "mcp");

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

        // Create OTLP gRPC service if configured - DCP binds to the port and proxies to the dashboard
        // This prevents port conflicts when the CLI exits and orphaned dashboard processes hold the port
        string? otlpGrpcServiceName = null;
        if (!string.IsNullOrEmpty(otlpGrpcUrl))
        {
            otlpGrpcServiceName = "aspire-dashboard-otlp-grpc";
            var otlpGrpcUri = new Uri(otlpGrpcUrl);
            var otlpGrpcServiceSpec = new DcpServiceSpec(
                Name: otlpGrpcServiceName,
                Port: otlpGrpcUri.Port,
                Address: "localhost",
                Protocol: "TCP",
                AddressAllocationMode: "Localhost");

            _logger.LogDebug("Creating OTLP gRPC service {ServiceName} with port {Port} (Localhost/proxy)", otlpGrpcServiceName, otlpGrpcUri.Port);
            await _dcpClient.CreateServiceAsync(otlpGrpcServiceSpec, cancellationToken);
        }

        // Create OTLP HTTP service if configured - DCP binds to the port and proxies to the dashboard
        string? otlpHttpServiceName = null;
        if (!string.IsNullOrEmpty(otlpHttpUrl))
        {
            otlpHttpServiceName = "aspire-dashboard-otlp-http";
            var otlpHttpUri = new Uri(otlpHttpUrl);
            var otlpHttpServiceSpec = new DcpServiceSpec(
                Name: otlpHttpServiceName,
                Port: otlpHttpUri.Port,
                Address: "localhost",
                Protocol: "TCP",
                AddressAllocationMode: "Localhost");

            _logger.LogDebug("Creating OTLP HTTP service {ServiceName} with port {Port} (Localhost/proxy)", otlpHttpServiceName, otlpHttpUri.Port);
            await _dcpClient.CreateServiceAsync(otlpHttpServiceSpec, cancellationToken);
        }

        // Use portForServing expression - DCP will resolve this to the actual port the dashboard should bind to
        var portExpression = $$$"""{{- portForServing "{{{serviceName}}}" -}}""";
        var aspnetCoreUrls = $"{scheme}://localhost:{portExpression}";

        // Create CLI config file for dynamic resource service URL updates
        // This file will be watched by the dashboard for configuration changes (IOptionsMonitor)
        var cliConfigPath = Path.Combine(dcpSession.SessionDir, "dashboard-config.json");

        // Initialize with empty config - the resource service URL will be written when AppHost reports it
        await File.WriteAllTextAsync(cliConfigPath, "{}", cancellationToken);
        _logger.LogDebug("Created CLI config file at {Path}", cliConfigPath);

        // Build dashboard environment variables
        var dashboardEnv = new Dictionary<string, string>
        {
            // Core URLs - use portForServing expression so DCP resolves the port at runtime
            ["ASPNETCORE_URLS"] = aspnetCoreUrls,
            ["ASPNETCORE_ENVIRONMENT"] = aspnetEnvironment,

            // CLI config file path - dashboard will watch this for resource service URL updates
            // This enables dynamic port binding: AppHost binds to port 0, reports actual URL,
            // CLI writes it to this file, dashboard picks up the change via IOptionsMonitor
            ["ASPIRE_CLI_CONFIG_PATH"] = cliConfigPath,

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
        // Use portForServing expressions so DCP binds to the external ports and proxies to the dashboard's internal ports
        // This prevents port conflicts when the CLI exits and orphaned dashboard processes hold the ports
        if (!string.IsNullOrEmpty(otlpGrpcUrl) && otlpGrpcServiceName != null)
        {
            var otlpGrpcUri = new Uri(otlpGrpcUrl);
            var otlpGrpcPortExpr = $$$"""{{- portForServing "{{{otlpGrpcServiceName}}}" -}}""";
            dashboardEnv["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = $"{otlpGrpcUri.Scheme}://localhost:{otlpGrpcPortExpr}";
            dashboardEnv["DASHBOARD__OTLP__AUTHMODE"] = "ApiKey";
            dashboardEnv["DASHBOARD__OTLP__PRIMARYAPIKEY"] = otlpApiKey;
        }

        if (!string.IsNullOrEmpty(otlpHttpUrl) && otlpHttpServiceName != null)
        {
            var otlpHttpUri = new Uri(otlpHttpUrl);
            var otlpHttpPortExpr = $$$"""{{- portForServing "{{{otlpHttpServiceName}}}" -}}""";
            dashboardEnv["ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL"] = $"{otlpHttpUri.Scheme}://localhost:{otlpHttpPortExpr}";
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

        // Check if dashboard already exists (handles apphost restart in watch mode)
        // The dashboard should persist across apphost restarts - it's CLI-owned, not apphost-owned
        const string dashboardName = "aspire-dashboard";
        try
        {
            var existingDashboard = await _dcpClient.GetExecutableAsync(dashboardName, cancellationToken);
            if (existingDashboard != null)
            {
                _logger.LogDebug("Dashboard already exists, skipping creation (apphost restart)");

                // Dashboard already running - just configure AppHost environment variables
                // with the same deterministic keys and return success
                var effectiveUrl = $"{scheme}://localhost:{desiredPort}";
                ConfigureAppHostEnvironmentVariables(
                    environmentVariables, effectiveUrl, browserToken, aspnetEnvironment,
                    launchProfile, otlpGrpcUrl, otlpHttpUrl, mcpUrl,
                    resourceServiceApiKey, otlpApiKey, mcpApiKey, dcpSession);

                _logger.LogDebug("CLI-owned dashboard reused at {DashboardUrl}", effectiveUrl);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking for existing dashboard (will create new one)");
        }

        _logger.LogDebug("Creating dashboard executable in DCP");

        // Create ServiceProducerAnnotation JSON to link the Executable to all Services
        // This tells DCP that this executable produces these services, enabling portForServing resolution
        var serviceProducers = new List<DcpServiceProducerAnnotation>
        {
            new() { ServiceName = serviceName, Address = "localhost" }  // HTTP frontend
        };
        if (otlpGrpcServiceName != null)
        {
            serviceProducers.Add(new() { ServiceName = otlpGrpcServiceName, Address = "localhost" });
        }
        if (otlpHttpServiceName != null)
        {
            serviceProducers.Add(new() { ServiceName = otlpHttpServiceName, Address = "localhost" });
        }
        var serviceProducerAnnotation = JsonSerializer.Serialize(
            serviceProducers.ToArray(),
            DcpJsonContext.Default.DcpServiceProducerAnnotationArray);

        var dashboardSpec = new DcpExecutableSpec(
            Name: dashboardName,
            ExecutablePath: "dotnet",
            WorkingDirectory: Path.GetDirectoryName(appHostInfo.DashboardPath),
            Args: [appHostInfo.DashboardPath],
            Env: dashboardEnv,
            Annotations: new Dictionary<string, string>
            {
                ["service-producer"] = serviceProducerAnnotation
            });

        await _dcpClient.CreateExecutableAsync(dashboardSpec, cancellationToken);

        // Wait for the dashboard executable to be running (this is the primary indicator)
        // The service state may remain null in some DCP configurations
        _logger.LogDebug("Waiting for dashboard executable to be running...");
        var dashboardReady = false;
        var maxWaitTime = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        await foreach (var execUpdate in _dcpClient.WatchExecutableAsync(dashboardName, cancellationToken))
        {
            _logger.LogDebug("Dashboard executable state: {State}", execUpdate.State);

            if (string.Equals(execUpdate.State, "Running", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Dashboard executable is running");
                dashboardReady = true;
                break;
            }

            if (string.Equals(execUpdate.State, "FailedToStart", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(execUpdate.State, "Terminated", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Dashboard executable failed to start: {State}", execUpdate.State);
                return false;
            }

            if (DateTime.UtcNow - startTime > maxWaitTime)
            {
                _logger.LogWarning("Timeout waiting for dashboard executable to start");
                return false;
            }
        }

        if (!dashboardReady)
        {
            _logger.LogWarning("Dashboard executable did not reach running state");
            return false;
        }

        // Give the dashboard a moment to bind its ports after reaching Running state
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

        // The effective URL for users is the proxy port (what DCP is listening on)
        var effectiveDashboardUrl = $"{scheme}://localhost:{desiredPort}";

        // Configure AppHost environment variables
        ConfigureAppHostEnvironmentVariables(
            environmentVariables, effectiveDashboardUrl, browserToken, aspnetEnvironment,
            launchProfile, otlpGrpcUrl, otlpHttpUrl, mcpUrl,
            resourceServiceApiKey, otlpApiKey, mcpApiKey, dcpSession);

        _logger.LogDebug("CLI-owned dashboard configured at {DashboardUrl}", effectiveDashboardUrl);
        return true;
    }

    /// <summary>
    /// Configures AppHost environment variables for CLI-owned dashboard mode.
    /// </summary>
    private void ConfigureAppHostEnvironmentVariables(
        IDictionary<string, string> environmentVariables,
        string effectiveDashboardUrl,
        string browserToken,
        string aspnetEnvironment,
        LaunchProfile launchProfile,
        string? otlpGrpcUrl,
        string? otlpHttpUrl,
        string? mcpUrl,
        string resourceServiceApiKey,
        string otlpApiKey,
        string mcpApiKey,
        DcpSession dcpSession)
    {
        // Configure AppHost with CLI dashboard info so backchannel returns correct URL
        environmentVariables["ASPIRE_CLI_DASHBOARD_MODE"] = "true";
        environmentVariables["ASPIRE_CLI_DASHBOARD_URL"] = effectiveDashboardUrl;
        environmentVariables["ASPIRE_CLI_DASHBOARD_TOKEN"] = browserToken;

        // Copy essential env vars from launchSettings since we use --no-launch-profile
        environmentVariables["ASPNETCORE_ENVIRONMENT"] = aspnetEnvironment;
        environmentVariables["DOTNET_ENVIRONMENT"] = aspnetEnvironment;

        // Set ASPNETCORE_URLS from applicationUrl - AppHost needs this for Kestrel binding
        // NOTE: We do NOT include the resource service endpoint URL here - the AppHost will bind to port 0 (dynamic)
        // and report the actual port via the backchannel. This avoids port conflicts during hot reload.
        var urls = new List<string>();
        if (!string.IsNullOrEmpty(launchProfile.ApplicationUrl))
        {
            urls.Add(launchProfile.ApplicationUrl);
        }
        // Intentionally NOT adding resourceServiceUrl - AppHost will use dynamic port
        if (urls.Count > 0)
        {
            environmentVariables["ASPNETCORE_URLS"] = string.Join(";", urls);
            _logger.LogDebug("AppHost ASPNETCORE_URLS set to: {Urls}", environmentVariables["ASPNETCORE_URLS"]);
        }

        // NOTE: We intentionally REMOVE ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL and DOTNET_RESOURCE_SERVICE_ENDPOINT_URL.
        // These may be set in launchSettings.json with fixed ports, which would cause port conflicts during hot reload.
        // By removing them, the AppHost's DashboardServiceHost will bind to port 0 (dynamic port allocation).
        // When the AppHost starts, it will report its actual resource service URL via the auxiliary backchannel,
        // and the CLI will write it to the dashboard-config.json file (ASPIRE_CLI_CONFIG_PATH).
        // The dashboard picks up the change via IOptionsMonitor and connects to the AppHost.
        //
        // This dynamic port approach avoids port conflicts during watch mode hot reloads:
        // - Old AppHost releases its dynamic port on shutdown
        // - New AppHost gets a fresh dynamic port
        // - Dashboard reconnects to the new port via config file reload
        environmentVariables.Remove("ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL");
        environmentVariables.Remove("DOTNET_RESOURCE_SERVICE_ENDPOINT_URL");

        // Store the session directory so the CLI can update the dashboard config when AppHost reports its resource service URL
        environmentVariables["ASPIRE_CLI_DCP_SESSION_DIR"] = dcpSession.SessionDir;
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
    }

    /// <summary>
    /// Updates the CLI config file with the resource service URL.
    /// This triggers the dashboard to reconnect to the new AppHost via IOptionsMonitor.
    /// </summary>
    /// <param name="sessionDir">The DCP session directory containing the config file.</param>
    /// <param name="resourceServiceUrl">The resource service URL to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task UpdateResourceServiceUrlAsync(string sessionDir, string resourceServiceUrl, CancellationToken cancellationToken = default)
    {
        var configPath = Path.Combine(sessionDir, "dashboard-config.json");

        // Write the config in the format expected by DashboardOptions configuration binding
        var config = $$"""
            {
              "Dashboard": {
                "ResourceServiceClient": {
                  "Url": "{{resourceServiceUrl}}"
                }
              }
            }
            """;

        await File.WriteAllTextAsync(configPath, config, cancellationToken);
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

    /// <summary>
    /// Generates a deterministic token based on a seed and purpose.
    /// This ensures the same token is generated across apphost restarts within the same DCP session.
    /// </summary>
    private static string GenerateDeterministicToken(string seed, string purpose)
    {
        var input = $"{seed}:{purpose}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
