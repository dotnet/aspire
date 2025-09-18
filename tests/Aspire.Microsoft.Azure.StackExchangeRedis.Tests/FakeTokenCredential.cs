// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;

namespace Aspire.Microsoft.Azure.StackExchangeRedis.Tests;

internal sealed class FakeTokenCredential : TokenCredential
{
    // For Redis, Azure AD authentication uses the 'oid' claim to identify the user
    public const string Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJJc3N1ZXIiOiJJc3N1ZXIiLCJJc3N1ZWQgQXQiOiIyMDI1LTAzLTIxVDAxOjM3OjAwLjE5OFoiLCJFeHBpcmF0aW9uIjoiMjAyNS0wMy0yMVQwMTozNzowMC4xOThaIiwib2lkIjoiMTIzNDU2NzgtOTBhYi1jZGVmLTEyMzQtNTY3ODkwYWJjZGVmIiwiUm9sZSI6IkFkbWluIn0.TKjH8_Ev-XNPvhkfPYm7RlKQl7n4nV-9UOKpQ3VzlZc";
    // {
    //   "Issuer": "Issuer",
    //   "Issued At": "2025-03-21T01:37:00.198Z",
    //   "Expiration": "2025-03-21T01:37:00.198Z",
    //   "oid": "12345678-90ab-cdef-1234-567890abcdef",
    //   "Role": "Admin"
    // }

    private readonly AccessToken _token;

    public FakeTokenCredential() : this(new AccessToken(Token, DateTimeOffset.UtcNow.AddHours(1)))
    {
    }

    public FakeTokenCredential(AccessToken token)
    {
        _token = token;
    }

    public bool IsGetTokenInvoked { get; private set; }

    public List<string> RequestedScopes { get; } = [];

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        RequestedScopes.AddRange(requestContext.Scopes);
        IsGetTokenInvoked = true;
        return _token;
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        RequestedScopes.AddRange(requestContext.Scopes);
        IsGetTokenInvoked = true;
        return new ValueTask<AccessToken>(_token);
    }
}
