// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Aspire.Dashboard.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Authentication.OtlpApiKey;

public class OtlpApiKeyAuthenticationHandler : AuthenticationHandler<OtlpApiKeyAuthenticationHandlerOptions>
{
    public const string ApiKeyHeaderName = "x-otlp-api-key";

    private readonly IOptionsMonitor<DashboardOptions> _dashboardOptions;

    public OtlpApiKeyAuthenticationHandler(IOptionsMonitor<DashboardOptions> dashboardOptions, IOptionsMonitor<OtlpApiKeyAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
        _dashboardOptions = dashboardOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options = _dashboardOptions.CurrentValue.Otlp;

        if (string.IsNullOrEmpty(options.PrimaryApiKey))
        {
            throw new InvalidOperationException("OTLP API key is not configured.");
        }

        if (Context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKey))
        {
            if (options.PrimaryApiKey != apiKey)
            {
                if (string.IsNullOrEmpty(options.SecondaryApiKey) || options.SecondaryApiKey != apiKey)
                {
                    return Task.FromResult(AuthenticateResult.Fail($"Incoming API key from '{ApiKeyHeaderName}' header doesn't match configured API key."));
                }
            }
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail($"API key from '{ApiKeyHeaderName}' header is missing."));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}

public static class OtlpApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "OtlpApiKey";
}

public sealed class OtlpApiKeyAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
}
