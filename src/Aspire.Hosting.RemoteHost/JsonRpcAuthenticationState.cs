// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.RemoteHost;

internal sealed class JsonRpcAuthenticationState
{
    private const string RemoteAppHostTokenEnvironmentVariableName = "ASPIRE_REMOTE_APPHOST_TOKEN";
    private readonly byte[]? _expectedTokenBytes;

    public JsonRpcAuthenticationState(IConfiguration configuration)
    {
        if (configuration[RemoteAppHostTokenEnvironmentVariableName] is { Length: > 0 } token)
        {
            _expectedTokenBytes = Encoding.UTF8.GetBytes(token);
        }

        IsAuthenticated = _expectedTokenBytes is null;
    }

    public bool IsAuthenticated { get; private set; }

    public bool Authenticate(string token)
    {
        if (IsAuthenticated)
        {
            return true;
        }

        if (_expectedTokenBytes is null)
        {
            IsAuthenticated = true;
            return true;
        }

        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var providedTokenBytes = Encoding.UTF8.GetBytes(token);

        try
        {
            var isMatch = providedTokenBytes.Length == _expectedTokenBytes.Length
                && CryptographicOperations.FixedTimeEquals(providedTokenBytes, _expectedTokenBytes);

            if (isMatch)
            {
                IsAuthenticated = true;
            }

            return isMatch;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(providedTokenBytes);
        }
    }

    public void ThrowIfNotAuthenticated()
    {
        if (!IsAuthenticated)
        {
            throw new InvalidOperationException("Client must authenticate before invoking AppHost RPC methods.");
        }
    }
}
