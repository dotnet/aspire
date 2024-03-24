// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Authentication;

public sealed class FrontendAuthenticationHandler(
    IOptionsMonitor<FrontendAuthenticationHandlerOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
        : AuthenticationHandler<FrontendAuthenticationHandlerOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        foreach (var scheme in GetRelevantAuthenticationSchemes())
        {
            var result = await Context.AuthenticateAsync(scheme).ConfigureAwait(false);

            if (result.Failure is not null)
            {
                return result;
            }
        }

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), Scheme.Name));

        static IEnumerable<string> GetRelevantAuthenticationSchemes()
        {
            yield return CookieAuthenticationDefaults.AuthenticationScheme;
            yield return OpenIdConnectDefaults.AuthenticationScheme;
        }
    }
}

public static class FrontendAuthenticationDefaults
{
    public const string AuthenticationScheme = "Frontend";
}

public static class FrontendAuthorizationDefaults
{
    public const string PolicyName = "Frontend";
}

public sealed class FrontendAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
}
