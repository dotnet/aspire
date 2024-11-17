// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal static class DashboardConfigNames
{
    public static readonly ConfigName DashboardFrontendUrlName = new("ASPNETCORE_URLS");
    public static readonly ConfigName DashboardOtlpGrpcUrlName = new("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL");
    public static readonly ConfigName DashboardOtlpHttpUrlName = new("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL");
    public static readonly ConfigName DashboardUnsecuredAllowAnonymousName = new("DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS");
    public static readonly ConfigName DashboardConfigFilePathName = new("DOTNET_DASHBOARD_CONFIG_FILE_PATH");
    public static readonly ConfigName DashboardFileConfigDirectoryName = new("DOTNET_DASHBOARD_FILE_CONFIG_DIRECTORY");
    public static readonly ConfigName ResourceServiceUrlName = new("DOTNET_RESOURCE_SERVICE_ENDPOINT_URL");

    public static readonly ConfigName DashboardOtlpAuthModeName = new("Dashboard:Otlp:AuthMode", "DASHBOARD__OTLP__AUTHMODE");
    public static readonly ConfigName DashboardOtlpPrimaryApiKeyName = new("Dashboard:Otlp:PrimaryApiKey", "DASHBOARD__OTLP__PRIMARYAPIKEY");
    public static readonly ConfigName DashboardOtlpSecondaryApiKeyName = new("Dashboard:Otlp:SecondaryApiKey", "DASHBOARD__OTLP__SECONDARYAPIKEY");
    public static readonly ConfigName DashboardOtlpCorsAllowedOriginsKeyName = new("Dashboard:Otlp:Cors:AllowedOrigins", "DASHBOARD__OTLP__CORS__ALLOWEDORIGINS");
    public static readonly ConfigName DashboardOtlpCorsAllowedHeadersKeyName = new("Dashboard:Otlp:Cors:AllowedHeaders", "DASHBOARD__OTLP__CORS__ALLOWEDHEADERS");
    public static readonly ConfigName DashboardOtlpAllowedCertificatesName = new("Dashboard:Otlp:AllowedCertificates", "DASHBOARD__OTLP__ALLOWEDCERTIFICATES");
    public static readonly ConfigName DashboardFrontendAuthModeName = new("Dashboard:Frontend:AuthMode", "DASHBOARD__FRONTEND__AUTHMODE");
    public static readonly ConfigName DashboardFrontendBrowserTokenName = new("Dashboard:Frontend:BrowserToken", "DASHBOARD__FRONTEND__BROWSERTOKEN");
    public static readonly ConfigName DashboardFrontendMaxConsoleLogCountName = new("Dashboard:Frontend:MaxConsoleLogCount", "DASHBOARD__FRONTEND__MAXCONSOLELOGCOUNT");
    public static readonly ConfigName ResourceServiceClientAuthModeName = new("Dashboard:ResourceServiceClient:AuthMode", "DASHBOARD__RESOURCESERVICECLIENT__AUTHMODE");
    public static readonly ConfigName ResourceServiceClientCertificateSourceName = new("Dashboard:ResourceServiceClient:ClientCertificate:Source", "DASHBOARD__RESOURCESERVICECLIENT__CLIENTCERTIFICATE__SOURCE");
    public static readonly ConfigName ResourceServiceClientCertificateFilePathName = new("Dashboard:ResourceServiceClient:ClientCertificate:FilePath", "DASHBOARD__RESOURCESERVICECLIENT__CLIENTCERTIFICATE__FILEPATH");
    public static readonly ConfigName ResourceServiceClientCertificateSubjectName = new("Dashboard:ResourceServiceClient:ClientCertificate:Subject", "DASHBOARD__RESOURCESERVICECLIENT__CLIENTCERTIFICATE__SUBJECT");
    public static readonly ConfigName ResourceServiceClientApiKeyName = new("Dashboard:ResourceServiceClient:ApiKey", "DASHBOARD__RESOURCESERVICECLIENT__APIKEY");
}

internal readonly struct ConfigName(string configKey, string? envVarName = null)
{
    public string ConfigKey { get; } = configKey;
    public string EnvVarName { get; } = envVarName ?? configKey;
}
