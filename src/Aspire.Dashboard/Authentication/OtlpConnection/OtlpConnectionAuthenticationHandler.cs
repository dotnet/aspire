// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Authentication.OtlpConnection;

public class OtlpConnectionAuthenticationHandler : AuthenticationHandler<OtlpConnectionAuthenticationHandlerOptions>
{
    public OtlpConnectionAuthenticationHandler(IOptionsMonitor<OtlpConnectionAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.Features.Get<IOtlpConnectionFeature>() == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("OTLP is not enabled on this connection."));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}

public static class OtlpConnectionAuthenticationDefaults
{
    public const string AuthenticationScheme = "OtlpConnection";
}

public sealed class OtlpConnectionAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
}
