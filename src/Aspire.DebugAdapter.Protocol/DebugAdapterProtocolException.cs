// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.DebugAdapter.Protocol;

/// <summary>
/// Exception thrown when a Debug Adapter Protocol violation is detected.
/// </summary>
/// <remarks>
/// This exception indicates a malformed message, invalid header, or other protocol-level error.
/// It is distinct from <see cref="DebugAdapterException"/> which represents a valid error response
/// from the debug adapter.
/// </remarks>
public class DebugAdapterProtocolException : Exception
{
    /// <summary>
    /// Creates a new protocol exception with the specified message.
    /// </summary>
    public DebugAdapterProtocolException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new protocol exception with the specified message and inner exception.
    /// </summary>
    public DebugAdapterProtocolException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
