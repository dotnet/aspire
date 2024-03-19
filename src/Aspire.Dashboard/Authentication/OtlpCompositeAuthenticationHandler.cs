// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Aspire.Dashboard.Authentication.OtlpApiKey;
using Aspire.Dashboard.Authentication.OtlpConnection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Authentication;

public sealed class OtlpCompositeAuthenticationHandler : AuthenticationHandler<OtlpCompositeAuthenticationHandlerOptions>
{
    public OtlpCompositeAuthenticationHandler(IOptionsMonitor<OtlpCompositeAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var connectionResult = await Context.AuthenticateAsync(OtlpConnectionAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        if (connectionResult.Failure != null)
        {
            return connectionResult;
        }

        var scheme = Options.OtlpAuthMode switch
        {
            OtlpAuthMode.ApiKey => OtlpApiKeyAuthenticationDefaults.AuthenticationScheme,
            OtlpAuthMode.ClientCertificate => CertificateAuthenticationDefaults.AuthenticationScheme,
            _ => null
        };

        if (scheme is not null)
        {
            var result = await Context.AuthenticateAsync(scheme).ConfigureAwait(false);
            if (result.Failure is not null)
            {
                return result;
            }
        }

        var id = new ClaimsIdentity([new Claim(OtlpAuthorization.OtlpClaimName, bool.TrueString)]);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), Scheme.Name));
    }
}

public static class OtlpCompositeAuthenticationDefaults
{
    public const string AuthenticationScheme = "OtlpComposite";
}

public sealed class OtlpCompositeAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
    public OtlpAuthMode OtlpAuthMode { get; set; }
}
