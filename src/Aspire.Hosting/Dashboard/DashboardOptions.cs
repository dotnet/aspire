// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal class DashboardOptions
{
    public string? DashboardPath { get; set; }
    public string? DashboardUrl { get; set; }
    public FrontendAuthMode DashboardAuthMode { get; set; }
    public string? DashboardToken { get; set; }
    public OpenIdConnectOptions? OpenIdConnect { get; set; } = new();
    public OpenIdConnectSettings? OpenIdConnectSettings { get; set; } = new();
    public string? OtlpGrpcEndpointUrl { get; set; }
    public string? OtlpHttpEndpointUrl { get; set; }
    public string? OtlpApiKey { get; set; }
    public string AspNetCoreEnvironment { get; set; } = "Production";
}

internal class OpenIdConnectSettings
{
    public string? Authority { get; set; }
    public string? MetadataAddress { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public ICollection<string> Scope { get; } = [];
}

internal class ConfigureDefaultDashboardOptions(IConfiguration configuration, IOptions<DcpOptions> dcpOptions) : IConfigureOptions<DashboardOptions>
{
    public void Configure(DashboardOptions options)
    {
        options.DashboardPath = dcpOptions.Value.DashboardPath;
        options.DashboardUrl = configuration[KnownConfigNames.AspNetCoreUrls];

        if (Enum.TryParse<FrontendAuthMode>(configuration[DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey], out var dashboardAuthMode))
        {
            options.DashboardAuthMode = dashboardAuthMode;
        }

        options.DashboardToken = configuration["AppHost:BrowserToken"];
        configuration.Bind("Dashboard:Frontend:OpenIdConnect", options.OpenIdConnect);
        configuration.Bind("Authentication:Schemes:OpenIdConnect", options.OpenIdConnectSettings);
        options.OtlpGrpcEndpointUrl = configuration[KnownConfigNames.DashboardOtlpGrpcEndpointUrl];
        options.OtlpHttpEndpointUrl = configuration[KnownConfigNames.DashboardOtlpHttpEndpointUrl];
        options.OtlpApiKey = configuration["AppHost:OtlpApiKey"];

        options.AspNetCoreEnvironment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
    }
}

internal class ValidateDashboardOptions : IValidateOptions<DashboardOptions>
{
    public ValidateOptionsResult Validate(string? name, DashboardOptions options)
    {
        var builder = new ValidateOptionsResultBuilder();

        if (string.IsNullOrEmpty(options.DashboardUrl))
        {
            builder.AddError("Failed to configure dashboard resource because ASPNETCORE_URLS environment variable was not set.");
        }

        if (string.IsNullOrEmpty(options.OtlpGrpcEndpointUrl) && string.IsNullOrEmpty(options.OtlpHttpEndpointUrl))
        {
            builder.AddError("Failed to configure dashboard resource because DOTNET_DASHBOARD_OTLP_ENDPOINT_URL and DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL environment variables are not set. At least one OTLP endpoint must be provided.");
        }

        return builder.Build();
    }
}
