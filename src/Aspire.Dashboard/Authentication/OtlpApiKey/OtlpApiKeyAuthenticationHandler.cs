// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Authentication.OtlpApiKey;

public class OtlpApiKeyAuthenticationHandler : AuthenticationHandler<OtlpApiKeyAuthenticationHandlerOptions>
{
    public const string ApiKeyHeaderName = "x-otlp-api-key";

    public OtlpApiKeyAuthenticationHandler(IOptionsMonitor<OtlpApiKeyAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(Options.OtlpApiKey))
        {
            throw new InvalidOperationException("OTLP API key is not configured.");
        }

        if (Context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKey))
        {
            if (Options.OtlpApiKey != apiKey)
            {
                return Task.FromResult(AuthenticateResult.Fail("Incoming API key doesn't match required API key."));
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
    public string? OtlpApiKey { get; set; }
}
