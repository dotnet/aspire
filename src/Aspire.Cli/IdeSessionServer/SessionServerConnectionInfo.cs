// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.IdeSessionServer;

/// <summary>
/// Connection information for the IDE session server.
/// This is passed to DCP so it can connect to the server.
/// </summary>
internal sealed class SessionServerConnectionInfo
{
    /// <summary>
    /// The port of the IDE session server (e.g., 12345).
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// The bearer token for authentication.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// The self-signed certificate in base64 DER format.
    /// </summary>
    public required string Certificate { get; init; }
}
