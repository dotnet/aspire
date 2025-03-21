// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;

namespace Aspire.Azure.Npgsql.Tests;

internal sealed class FakeTokenCredential : TokenCredential
{
    private const string Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJJc3N1ZXIiOiJJc3N1ZXIiLCJJc3N1ZWQgQXQiOiIyMDI1LTAzLTIxVDAxOjM3OjAwLjE5OFoiLCJFeHBpcmF0aW9uIjoiMjAyNS0wMy0yMVQwMTozNzowMC4xOThaIiwicHJlZmVycmVkX3VzZXJuYW1lIjoibWlrZXlAbW91c2UuY29tIiwiUm9sZSI6IkFkbWluIn0.WdKzHL5CBeMOIlpzqaotvNbo0mmvaMtcW0zpMlB3lUE";

    // {
    //   "Issuer": "Issuer",
    //   "Issued At": "2025-03-21T01:37:00.198Z",
    //   "Expiration": "2025-03-21T01:37:00.198Z",
    //   "preferred_username": "mikey@mouse.com",
    //   "Role": "Admin"
    // }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken(Token, DateTimeOffset.Now);
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(new AccessToken(Token, DateTimeOffset.Now));
    }
}
