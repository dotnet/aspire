// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal static class KnownConfigNames
{
    public const string AspNetCoreUrls = "ASPNETCORE_URLS";
    public const string AllowUnsecuredTransport = "ASPIRE_ALLOW_UNSECURED_TRANSPORT";
    public const string DashboardOtlpGrpcEndpointUrl = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    public const string DashboardOtlpHttpEndpointUrl = "DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL";
    public const string DashboardFrontendBrowserToken = "DOTNET_DASHBOARD_FRONTEND_BROWSERTOKEN";
    public const string DashboardResourceServiceClientApiKey = "DOTNET_DASHBOARD_RESOURCESERVICE_APIKEY";
    public const string DashboardUnsecuredAllowAnonymous = "DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS";
    public const string DashboardCorsAllowedOrigins = "DOTNET_DASHBOARD_CORS_ALLOWED_ORIGINS";
    public const string ResourceServiceEndpointUrl = "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL";
}
