// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Aspire.Dashboard.Authentication.Connection;
using Aspire.Dashboard.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Authentication;

public sealed class FrontendCompositeAuthenticationHandler(
    IOptionsMonitor<DashboardOptions> dashboardOptions,
    IOptionsMonitor<FrontendCompositeAuthenticationHandlerOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
        : AuthenticationHandler<FrontendCompositeAuthenticationHandlerOptions>(options, logger, encoder)
{
    private const string SuppressChallengeKey = "SuppressChallenge";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var result = await Context.AuthenticateAsync(ConnectionTypeAuthenticationDefaults.AuthenticationSchemeFrontend).ConfigureAwait(false);
        if (result.Failure is not null)
        {
            return AuthenticateResult.Fail(
                result.Failure,
                new AuthenticationProperties(new Dictionary<string, string?>(),
                new Dictionary<string, object?> { [SuppressChallengeKey] = true }));
        }

        var scheme = GetRelevantAuthenticationScheme();
        if (scheme == null)
        {
            var id = new ClaimsIdentity([new Claim(OtlpAuthorization.OtlpClaimName, bool.FalseString)]);
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), Scheme.Name));
        }

        result = await Context.AuthenticateAsync().ConfigureAwait(false);
        return result;
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // If the connection type is wrong then always return unauthorized.
        if (properties.GetParameter<bool>(SuppressChallengeKey))
        {
            return;
        }

        var scheme = GetRelevantAuthenticationScheme();
        if (scheme != null)
        {
            await Context.ChallengeAsync(scheme).ConfigureAwait(false);
        }
    }

    private string? GetRelevantAuthenticationScheme()
    {
        var options = dashboardOptions.CurrentValue;

        if (options.Frontend.AuthMode is FrontendAuthMode.OpenIdConnect)
        {
            return FrontendAuthenticationDefaults.AuthenticationSchemeOpenIdConnect;
        }
        else if (options.Frontend.AuthMode is FrontendAuthMode.BrowserToken)
        {
            return FrontendAuthenticationDefaults.AuthenticationSchemeBrowserToken;
        }

        return null;
    }
}

public static class FrontendCompositeAuthenticationDefaults
{
    public const string AuthenticationScheme = "FrontendComposite";
}

public sealed class FrontendCompositeAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
}
