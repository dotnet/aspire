// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dashboard;

internal static class ResourceServiceApiKeyAuthorization
{
    public const string PolicyName = "ResourceServiceApiKeyPolicy";
}

internal static class ResourceServiceApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "ResourceServiceApiKey";
}

internal sealed class ResourceServiceApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

internal sealed class ResourceServiceApiKeyAuthenticationHandler(
    IOptionsMonitor<ResourceServiceOptions> resourceServiceOptions,
    IOptionsMonitor<ResourceServiceApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ResourceServiceApiKeyAuthenticationOptions>(options, logger, encoder)
{
    private const string ApiKeyHeaderName = "x-resource-service-api-key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options = resourceServiceOptions.CurrentValue;

        if (options.AuthMode is ResourceServiceAuthMode.ApiKey)
        {
            if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerValues))
            {
                return Task.FromResult(AuthenticateResult.Fail($"'{ApiKeyHeaderName}' header not found"));
            }

            if (headerValues.Count != 1)
            {
                return Task.FromResult(AuthenticateResult.Fail($"Expecting only a single '{ApiKeyHeaderName}' header."));
            }

            if (!CompareHelpers.CompareKey(expectedKeyBytes: options.GetApiKeyBytes(), requestKey: headerValues.ToString()))
            {
                return Task.FromResult(AuthenticateResult.Fail($"Invalid '{ApiKeyHeaderName}' header value."));
            }
        }

        return Task.FromResult(
            AuthenticateResult.Success(
                new AuthenticationTicket(
                    principal: new ClaimsPrincipal(new ClaimsIdentity(
                        claims: [],
                        authenticationType: ResourceServiceApiKeyAuthenticationDefaults.AuthenticationScheme)),
                    authenticationScheme: Scheme.Name)));
    }
}
