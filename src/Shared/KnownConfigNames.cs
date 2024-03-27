// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal static class KnownConfigNames
{
    public static string AspNetCoreUrls = "ASPNETCORE_URLS";
    public static string AllowUnsecuredTransport = "ASPIRE_ALLOW_UNSECURED_TRANSPORT";
    public static string DashboardOtlpEndpointUrl = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL";
    public static string ResourceServiceEndpointUrl = "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL";
}
