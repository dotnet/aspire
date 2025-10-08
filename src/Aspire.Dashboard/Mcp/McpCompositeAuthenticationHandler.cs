// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Aspire.Dashboard.Authentication.Connection;
using Aspire.Dashboard.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Mcp;

public sealed class McpCompositeAuthenticationHandler(
    IOptionsMonitor<DashboardOptions> dashboardOptions,
    IOptionsMonitor<McpCompositeAuthenticationHandlerOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
        : AuthenticationHandler<McpCompositeAuthenticationHandlerOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options = dashboardOptions.CurrentValue;

        foreach (var scheme in GetRelevantAuthenticationSchemes())
        {
            var result = await Context.AuthenticateAsync(scheme).ConfigureAwait(false);

            if (result.Failure is not null)
            {
                return result;
            }
        }

        var id = new ClaimsIdentity([new Claim(McpApiKeyAuthenticationHandler.McpClaimName, bool.TrueString)]);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), Scheme.Name));

        IEnumerable<string> GetRelevantAuthenticationSchemes()
        {
            yield return ConnectionTypeAuthenticationDefaults.AuthenticationSchemeMcp;

            if (options.Mcp.AuthMode is McpAuthMode.ApiKey)
            {
                yield return McpApiKeyAuthenticationHandler.AuthenticationScheme;
            }
        }
    }
}

public static class McpCompositeAuthenticationDefaults
{
    public const string AuthenticationScheme = "McpComposite";
}

public sealed class McpCompositeAuthenticationHandlerOptions : AuthenticationSchemeOptions
{
}
