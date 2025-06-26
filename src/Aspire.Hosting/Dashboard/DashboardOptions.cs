// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal class DashboardOptions
{
    public string? DashboardPath { get; set; }
    public string? DashboardUrl { get; set; }
    public string? DashboardToken { get; set; }
    public string? OtlpGrpcEndpointUrl { get; set; }
    public string? OtlpHttpEndpointUrl { get; set; }
    public string? OtlpApiKey { get; set; }
    public string? ResourceServiceUrl { get; set; }
    public string AspNetCoreEnvironment { get; set; } = "Production";
    public bool? TelemetryOptOut { get; set; }
}

internal class ConfigureDefaultDashboardOptions(IConfiguration configuration, IOptions<DcpOptions> dcpOptions) : IConfigureOptions<DashboardOptions>
{
    // We default to HTTPS here because the dashboard is expected to be hosted in a secure environment and we require users to opt-in to unsecured transport
    // via the `ASPIRE_ALLOW_UNSECURED_TRANSPORT` environment variable. If there is no dev
    private const string DashboardUrlsDefault = "https://localhost:18887;http://localhost:18888";
    private const string DashboardOtlpGrpcEndpointUrlDefault = "https://localhost:18890";

    public void Configure(DashboardOptions options)
    {
        options.DashboardPath = dcpOptions.Value.DashboardPath;
        options.DashboardUrl = configuration[KnownConfigNames.AspNetCoreUrls] ?? DashboardUrlsDefault;
        options.DashboardToken = configuration["AppHost:BrowserToken"];

        options.OtlpGrpcEndpointUrl = configuration.GetString(KnownConfigNames.DashboardOtlpGrpcEndpointUrl, KnownConfigNames.Legacy.DashboardOtlpGrpcEndpointUrl);
        options.OtlpGrpcEndpointUrl ??= DashboardOtlpGrpcEndpointUrlDefault;
        options.OtlpHttpEndpointUrl = configuration.GetString(KnownConfigNames.DashboardOtlpHttpEndpointUrl, KnownConfigNames.Legacy.DashboardOtlpHttpEndpointUrl);
        options.OtlpApiKey = configuration["AppHost:OtlpApiKey"];

        options.ResourceServiceUrl = configuration.GetString(KnownConfigNames.ResourceServiceEndpointUrl, KnownConfigNames.Legacy.ResourceServiceEndpointUrl);

        options.AspNetCoreEnvironment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";

        options.TelemetryOptOut = bool.TryParse(configuration["ASPIRE_DASHBOARD_TELEMETRY_OPTOUT"], out var telemetryOptOut)
            ? !telemetryOptOut
            : null;
    }
}
