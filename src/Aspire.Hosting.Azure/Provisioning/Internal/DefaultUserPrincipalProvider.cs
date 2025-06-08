// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Azure.Core;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IUserPrincipalProvider"/>.
/// </summary>
internal sealed class DefaultUserPrincipalProvider(ITokenCredentialProvider tokenCredentialProvider) : IUserPrincipalProvider
{
    public async Task<UserPrincipal> GetUserPrincipalAsync(CancellationToken cancellationToken = default)
    {
        var credential = tokenCredentialProvider.GetTokenCredential();
        var response = await credential.GetTokenAsync(new(["https://graph.windows.net/.default"]), cancellationToken).ConfigureAwait(false);

        static UserPrincipal ParseToken(in AccessToken response)
        {
            // Parse the access token to get the user's object id (this is their principal id)
            var oid = string.Empty;
            var upn = string.Empty;
            var parts = response.Token.Split('.');
            var part = parts[1];
            var convertedToken = part.ToString().Replace('_', '/').Replace('-', '+');

            switch (part.Length % 4)
            {
                case 2:
                    convertedToken += "==";
                    break;
                case 3:
                    convertedToken += "=";
                    break;
            }
            var bytes = Convert.FromBase64String(convertedToken);
            Utf8JsonReader reader = new(bytes);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var header = reader.GetString();
                    if (header == "oid")
                    {
                        reader.Read();
                        oid = reader.GetString()!;
                        if (!string.IsNullOrEmpty(upn))
                        {
                            break;
                        }
                    }
                    else if (header is "upn" or "email")
                    {
                        reader.Read();
                        upn = reader.GetString()!;
                        if (!string.IsNullOrEmpty(oid))
                        {
                            break;
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                }
            }
            return new UserPrincipal(Guid.Parse(oid), upn);
        }

        return ParseToken(response);
    }
}