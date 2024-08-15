// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Authentication.Connection;

public class ConnectionTypeAuthenticationHandler : AuthenticationHandler<ConnectionTypeAuthenticationHandlerOptions>
{
    public ConnectionTypeAuthenticationHandler(IOptionsMonitor<ConnectionTypeAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var connectionTypeFeature = Context.Features.Get<IConnectionTypeFeature>();

        if (connectionTypeFeature == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("No type specified on this connection."));
        }

        if (!connectionTypeFeature.ConnectionTypes.Contains(Options.RequiredConnectionType))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Connection type {Options.RequiredConnectionType} is not enabled on this connection."));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}

public static class ConnectionTypeAuthenticationDefaults
{
    public const string AuthenticationSchemeFrontend = "ConnectionFrontend";
    public const string AuthenticationSchemeOtlp = "ConnectionOtlp";
}

public sealed class ConnectionTypeAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
    public ConnectionType RequiredConnectionType { get; set; }
}
