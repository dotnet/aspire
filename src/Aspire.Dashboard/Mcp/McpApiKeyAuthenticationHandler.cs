// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Aspire.Dashboard.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Mcp;

public class McpApiKeyAuthenticationHandler : AuthenticationHandler<McpApiKeyAuthenticationHandlerOptions>
{
    public const string PolicyName = "McpPolicy";
    public const string McpClaimName = "McpClaim";

    public const string AuthenticationScheme = "McpApiKey";
    public const string ApiKeyHeaderName = "x-mcp-api-key";

    private readonly IOptionsMonitor<DashboardOptions> _dashboardOptions;

    public McpApiKeyAuthenticationHandler(IOptionsMonitor<DashboardOptions> dashboardOptions, IOptionsMonitor<McpApiKeyAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
        _dashboardOptions = dashboardOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options = _dashboardOptions.CurrentValue.Mcp;

        if (Context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKey))
        {
            // There must be only one header with the API key.
            if (apiKey.Count != 1)
            {
                return Task.FromResult(AuthenticateResult.Fail($"Multiple '{ApiKeyHeaderName}' headers in request."));
            }

            if (!CompareHelpers.CompareKey(options.GetPrimaryApiKeyBytes(), apiKey.ToString()))
            {
                if (options.GetSecondaryApiKeyBytes() is not { } secondaryBytes || !CompareHelpers.CompareKey(secondaryBytes, apiKey.ToString()))
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

public sealed class McpApiKeyAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
}
