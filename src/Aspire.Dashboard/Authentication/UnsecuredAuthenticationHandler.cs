// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Aspire.Dashboard.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Authentication;

public class UnsecuredAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public UnsecuredAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var id = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "Local"), new Claim(FrontendAuthorizationDefaults.UnsecuredClaimName, bool.TrueString)],
            FrontendAuthenticationDefaults.AuthenticationSchemeUnsecured);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), Scheme.Name)));
    }
}
