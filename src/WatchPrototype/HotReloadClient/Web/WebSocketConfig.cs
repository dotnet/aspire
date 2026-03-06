// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.DotNet.HotReload;

internal readonly struct WebSocketConfig(int port, int? securePort, string? hostName)
{
    /// <summary>
    /// 0 to auto-assign.
    /// </summary>
    public int Port => port;

    /// <summary>
    /// 0 to auto-assign, null to disable HTTPS/WSS.
    /// </summary>
    public int? SecurePort => securePort;

    // Use 127.0.0.1 instead of "localhost" because Kestrel doesn't support dynamic port binding with "localhost".
    // System.InvalidOperationException: Dynamic port binding is not supported when binding to localhost.
    // You must either bind to 127.0.0.1:0 or [::1]:0, or both.
    public string HostName => hostName ?? "127.0.0.1";

    public IEnumerable<string> GetHttpUrls()
    {
        yield return $"http://{HostName}:{Port}";

        if (SecurePort.HasValue)
        {
            yield return $"https://{HostName}:{SecurePort.Value}";
        }
    }

    public WebSocketConfig WithSecurePort(int? value)
        => new(port, value, hostName);
}
