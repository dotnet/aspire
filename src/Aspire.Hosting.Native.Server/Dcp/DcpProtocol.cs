// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Hosting.Native.Dcp;

internal enum DcpResponseMode { Json, Empty }

/// <summary>Parses DCP log payloads independently of HTTP and retained log cursors.</summary>
internal static class DcpProtocol
{
    public static string[] ParseCompleteLogLines(ReadOnlySpan<byte> bytes)
    {
        // Cursors count complete lines, not bytes. For "ready\npart", leave
        // the unfinished tail at the same cursor instead of duplicating it.
        var completeLength = bytes.LastIndexOf((byte)'\n') + 1;

        return Encoding.UTF8.GetString(bytes[..completeLength]).Split('\n', StringSplitOptions.None)[..^1];
    }
}
