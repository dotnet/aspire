// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;
using Azure.Core;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Azure;

internal sealed class UserSecretsTokenCredential(IConfiguration configuration, string userSecretsPath, TokenCredential tokenCredential) : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return GetTokenAsyncCore(requestContext, sync: true, cancellationToken).AsTask().GetAwaiter().GetResult();
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return GetTokenAsyncCore(requestContext, sync: false, cancellationToken);
    }

    private async ValueTask<AccessToken> GetTokenAsyncCore(TokenRequestContext requestContext, bool sync, CancellationToken cancellationToken)
    {
        // Key the credentials by scope (everything else seems to change)
        var key = requestContext.Scopes switch
        {
            [] => "",
            [string s] => s,
            _ => string.Join(",", requestContext.Scopes)
        };

        var credentials = configuration.GetSection($"Azure:Credentials:{key}");

        var tokenValue = credentials["AccessToken"];
        var expiresOnValue = credentials["ExpiresOn"];

        DateTimeOffset expiresOn = default;

        if (tokenValue is null || (expiresOnValue is not null && DateTimeOffset.TryParse(expiresOnValue, CultureInfo.InvariantCulture, out expiresOn)
            && expiresOn < DateTimeOffset.UtcNow))
        {
            var accessToken = sync
                ? tokenCredential.GetToken(requestContext, cancellationToken)
                : await tokenCredential.GetTokenAsync(requestContext, cancellationToken).ConfigureAwait(false);

            tokenValue = accessToken.Token;
            expiresOn = accessToken.ExpiresOn;

            var allSecrets = JsonNode.Parse(File.ReadAllText(userSecretsPath))!.AsObject();

            var section = allSecrets.Prop("Azure").Prop("Credentials").Prop(key);

            // REVIEW: Do we need to protect the token at rest?
            section["AccessToken"] = tokenValue;
            section["ExpiresOn"] = expiresOn.ToString(CultureInfo.InvariantCulture);

            // Modified secrets with access token
            File.WriteAllText(userSecretsPath, allSecrets.ToString());

            // Reload configuration
            (configuration as IConfigurationRoot)?.Reload();
        }

        return new(tokenValue, expiresOn);
    }
}
