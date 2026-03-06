// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// A response read from the transport, containing the response type and a stream to read the response data from.
/// </summary>
/// <param name="type">The response type.</param>
/// <param name="data">Stream to read response data from.</param>
/// <param name="disposeStream">Whether the stream should be disposed after reading.</param>
internal readonly struct ClientTransportResponse(ResponseType type, Stream data, bool disposeStream) : IDisposable
{
    public ResponseType Type => type;
    public Stream Data => data;

    public void Dispose()
    {
        if (disposeStream)
        {
            data.Dispose();
        }
    }
}
