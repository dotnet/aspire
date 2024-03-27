// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal class TransportOptionsValidator(IConfiguration configuration, DistributedApplicationExecutionContext executionContext) : IValidateOptions<TransportOptions>
{
    public ValidateOptionsResult Validate(string? name, TransportOptions options)
    {
        if (executionContext.IsPublishMode)
        {
            return ValidateOptionsResult.Success;
        }

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

        if (parsedFirstApplicationUrl.Scheme == "http" && !options.AllowUnsecureTransport)
        {
            return ValidateOptionsResult.Fail($"The 'applicationUrl' setting must be an https address unless the '{KnownConfigNames.AllowUnsecuredTransport}' environment variable is set to true. This configuration is commonly set in the launch profile. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.");
        }

        return ValidateOptionsResult.Success;
    }
}
