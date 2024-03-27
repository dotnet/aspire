// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Configuration;

public sealed class PostConfigureDashboardOptions : IPostConfigureOptions<DashboardOptions>
{
    private readonly IConfiguration _configuration;

    public PostConfigureDashboardOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void PostConfigure(string? name, DashboardOptions options)
    {
        // Copy aliased config values to the strongly typed options.
        if (_configuration[DashboardConfigNames.DashboardOtlpUrlName.ConfigKey] is { Length: > 0 } otlpUrl)
        {
            options.Otlp.EndpointUrl = otlpUrl;
        }
        if (_configuration[DashboardConfigNames.DashboardFrontendUrlName.ConfigKey] is { Length: > 0 } frontendUrls)
        {
            options.Frontend.EndpointUrls = frontendUrls;
        }
        if (_configuration[DashboardConfigNames.ResourceServiceUrlName.ConfigKey] is { Length: > 0 } resourceServiceUrl)
        {
            options.ResourceServiceClient.Url = resourceServiceUrl;
        }
        if (_configuration.GetBool(DashboardConfigNames.DashboardInsecureAllowAnonymousName.ConfigKey) ?? false)
        {
            options.Frontend.AuthMode = FrontendAuthMode.Unsecured;
            options.Otlp.AuthMode = OtlpAuthMode.Unsecured;
        }
    }
}
