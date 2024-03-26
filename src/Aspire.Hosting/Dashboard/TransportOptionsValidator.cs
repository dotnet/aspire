// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal class TransportOptionsValidator(IConfiguration configuration) : IValidateOptions<TransportOptions>
{
    public ValidateOptionsResult Validate(string? name, TransportOptions options)
    {
        if (configuration[KnownEnvironmentVariables.AspNetCoreUrls] is not { } applicationUrls)
        {
            throw new DistributedApplicationException($"AppHost does not have applicationUrl in launch profile, or {KnownEnvironmentVariables.AspNetCoreUrls} environment variable set.");
        }

        var firstApplicationUrl = applicationUrls.Split(";").First();

        if (!Uri.TryCreate(firstApplicationUrl, UriKind.Absolute, out var parsedFirstApplicationUrl))
        {
            throw new DistributedApplicationException($"The 'applicationUrl' setting of the launch profile has value '{firstApplicationUrl}' which could not be parsed as a URI.");
        }

        if (parsedFirstApplicationUrl.Scheme == "http" && !options.AllowUnsecureTransport.GetValueOrDefault(false))
        {
            throw new DistributedApplicationException($"Use of a non-TLS URL in the 'applicationUrl' setting of the launch profile unless the 'ASPIRE_ALLOW_UNSECURED_TRANSPORT' environment variable is set to 'true'. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.");
        }

        _ = configuration;
        return ValidateOptionsResult.Success;
    }
}
