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

    private const string ManagedIdentityToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJJc3N1ZXIiOiJJc3N1ZXIiLCJJc3N1ZWQgQXQiOiIyMDI1LTAzLTIxVDAxOjM3OjAwLjE5OFoiLCJFeHBpcmF0aW9uIjoiMjAyNS0wMy0yMVQwMTozNzowMC4xOThaIiwieG1zX21pcmlkIjoiL3N1YnNjcmlwdGlvbnMvMTIzL3Jlc291cmNlZ3JvdXBzL3JnLTEyMy9wcm92aWRlcnMvTWljcm9zb2Z0Lk1hbmFnZWRJZGVudGl0eS91c2VyQXNzaWduZWRJZGVudGl0aWVzL21pLTEyMyIsIlJvbGUiOiJBZG1pbiJ9.luuw0374yNSOWKfswHURCm620UoY9qrZriqLG0668Tw";
    // {
    //  "Issuer": "Issuer",
    //  "Issued At": "2025-03-21T01:37:00.198Z",
    //  "Expiration": "2025-03-21T01:37:00.198Z",
    //  "xms_mirid": "/subscriptions/123/resourcegroups/rg-123/providers/Microsoft.ManagedIdentity/userAssignedIdentities/mi-123",
    //  "Role": "Admin"
    //}

    private readonly AccessToken _token;

    public FakeTokenCredential(bool useManagedIdentity = false) :
        this(new AccessToken(useManagedIdentity ? ManagedIdentityToken : Token, DateTime.UtcNow))
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
