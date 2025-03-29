// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal static class KnownConfigNames
{
    public const string AspNetCoreUrls = "ASPNETCORE_URLS";
    public const string AllowUnsecuredTransport = "ASPIRE_ALLOW_UNSECURED_TRANSPORT";
    public const string DashboardOtlpGrpcEndpointUrl = "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL";
    public const string DashboardOtlpHttpEndpointUrl = "ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL";
    public const string DashboardFrontendBrowserToken = "ASPIRE_DASHBOARD_FRONTEND_BROWSERTOKEN";
    public const string DashboardResourceServiceClientApiKey = "ASPIRE_DASHBOARD_RESOURCESERVICE_APIKEY";
    public const string DashboardUnsecuredAllowAnonymous = "ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS";
    public const string DashboardCorsAllowedOrigins = "ASPIRE_DASHBOARD_CORS_ALLOWED_ORIGINS";
    public const string DashboardConfigFilePath = "ASPIRE_DASHBOARD_CONFIG_FILE_PATH";
    public const string DashboardFileConfigDirectory = "ASPIRE_DASHBOARD_FILE_CONFIG_DIRECTORY";

    public const string ShowDashboardResources = "ASPIRE_SHOW_DASHBOARD_RESOURCES";
    public const string ResourceServiceEndpointUrl = "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL";

    public const string ContainerRuntime = "ASPIRE_CONTAINER_RUNTIME";
    public const string DependencyCheckTimeout = "ASPIRE_DEPENDENCY_CHECK_TIMEOUT";
    public const string ServiceStartupWatchTimeout = "ASPIRE_SERVICE_STARTUP_WATCH_TIMEOUT";
    public const string WaitForDebugger = "ASPIRE_WAIT_FOR_DEBUGGER";
    public const string WaitForDebuggerTimeout = "ASPIRE_DEBUGGER_TIMEOUT";
    public const string UnixSocketPath = "ASPIRE_BACKCHANNEL_PATH";
    public const string CliProcessId = "ASPIRE_CLI_PID";
    public const string ForceRichConsole = "ASPIRE_FORCE_RICH_CONSOLE";
    public const string TestingDisableHttpClient = "ASPIRE_TESTING_DISABLE_HTTP_CLIENT";

    public static class Legacy
    {
        public const string DashboardOtlpGrpcEndpointUrl = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
        public const string DashboardOtlpHttpEndpointUrl = "DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL";
        public const string DashboardFrontendBrowserToken = "DOTNET_DASHBOARD_FRONTEND_BROWSERTOKEN";
        public const string DashboardResourceServiceClientApiKey = "DOTNET_DASHBOARD_RESOURCESERVICE_APIKEY";
        public const string DashboardUnsecuredAllowAnonymous = "DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS";
        public const string DashboardCorsAllowedOrigins = "DOTNET_DASHBOARD_CORS_ALLOWED_ORIGINS";
        public const string DashboardConfigFilePath = "DOTNET_DASHBOARD_CONFIG_FILE_PATH";
        public const string DashboardFileConfigDirectory = "DOTNET_DASHBOARD_FILE_CONFIG_DIRECTORY";

        public const string ShowDashboardResources = "DOTNET_SHOW_DASHBOARD_RESOURCES";
        public const string ResourceServiceEndpointUrl = "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL";

        public const string ContainerRuntime = "DOTNET_ASPIRE_CONTAINER_RUNTIME";
        public const string DependencyCheckTimeout = "DOTNET_ASPIRE_DEPENDENCY_CHECK_TIMEOUT";
        public const string ServiceStartupWatchTimeout = "DOTNET_ASPIRE_SERVICE_STARTUP_WATCH_TIMEOUT";
    }
}
