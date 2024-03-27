// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal class TransportOptionsValidator(IConfiguration configuration, DistributedApplicationExecutionContext executionContext, DistributedApplicationOptions distributedApplicationOptions) : IValidateOptions<TransportOptions>
{
    public ValidateOptionsResult Validate(string? name, TransportOptions transportOptions)
    {
        var effectiveAllowUnsecureTransport = transportOptions.AllowUnsecureTransport || distributedApplicationOptions.DisableDashboard || distributedApplicationOptions.AllowUnsecuredTransport;

        if (executionContext.IsPublishMode || effectiveAllowUnsecureTransport)
        {
            return ValidateOptionsResult.Success;
        }

        // Validate ASPNETCORE_URLS
        var applicationUrls = configuration[KnownConfigNames.AspNetCoreUrls];
        if (string.IsNullOrEmpty(applicationUrls))
        {
            return ValidateOptionsResult.Fail($"AppHost does not have applicationUrl in launch profile, or {KnownConfigNames.AspNetCoreUrls} environment variable set.");
        }

        var firstApplicationUrl = applicationUrls.Split(";").First();

        if (!Uri.TryCreate(firstApplicationUrl, UriKind.Absolute, out var parsedFirstApplicationUrl))
        {
            return ValidateOptionsResult.Fail($"The 'applicationUrl' setting of the launch profile has value '{firstApplicationUrl}' which could not be parsed as a URI.");
        }

        if (parsedFirstApplicationUrl.Scheme == "http")
        {
            return ValidateOptionsResult.Fail($"The 'applicationUrl' setting must be an https address unless the '{KnownConfigNames.AllowUnsecuredTransport}' environment variable is set to true. This configuration is commonly set in the launch profile. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.");
        }

        // Vaidate DOTNET_DASHBOARD_OTLP_ENDPOINT_URL
        var dashboardOtlpEndpointUrl = configuration[KnownConfigNames.DashboardOtlpEndpointUrl];
        if (string.IsNullOrEmpty(dashboardOtlpEndpointUrl))
        {
            return ValidateOptionsResult.Fail($"AppHost does not have the {KnownConfigNames.DashboardOtlpEndpointUrl} setting defined.");
        }

        if (!Uri.TryCreate(dashboardOtlpEndpointUrl, UriKind.Absolute, out var parsedDashboardOtlpEndpointUrl))
        {
            return ValidateOptionsResult.Fail($"The {KnownConfigNames.DashboardOtlpEndpointUrl} setting with a value of '{dashboardOtlpEndpointUrl}' could not be parsed as a URI.");
        }

        if (parsedDashboardOtlpEndpointUrl.Scheme == "http")
        {
            return ValidateOptionsResult.Fail($"The '{KnownConfigNames.DashboardOtlpEndpointUrl}' setting must be an https address unless the '{KnownConfigNames.AllowUnsecuredTransport}' environment variable is set to true. This configuration is commonly set in the launch profile. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.");
        }

        // Vaidate DOTNET_DASHBOARD_RESOURCE_SERVER_ENDPOINT_URL
        var resourceServiceEndpointUrl = configuration[KnownConfigNames.ResourceServiceEndpointUrl];
        if (string.IsNullOrEmpty(resourceServiceEndpointUrl))
        {
            return ValidateOptionsResult.Fail($"AppHost does not have the {KnownConfigNames.ResourceServiceEndpointUrl} setting defined.");
        }

        if (!Uri.TryCreate(resourceServiceEndpointUrl, UriKind.Absolute, out var parsedResourceServiceEndpointUrl))
        {
            return ValidateOptionsResult.Fail($"The {KnownConfigNames.ResourceServiceEndpointUrl} setting with a value of '{resourceServiceEndpointUrl}' could not be parsed as a URI.");
        }

        if (parsedResourceServiceEndpointUrl.Scheme == "http")
        {
            return ValidateOptionsResult.Fail($"The '{KnownConfigNames.ResourceServiceEndpointUrl}' setting must be an https address unless the '{KnownConfigNames.AllowUnsecuredTransport}' environment variable is set to true. This configuration is commonly set in the launch profile. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.");
        }

        return ValidateOptionsResult.Success;
    }
}
