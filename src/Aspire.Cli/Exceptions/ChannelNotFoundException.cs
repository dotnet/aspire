// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Exceptions;

/// <summary>
/// Exception thrown when a specified channel name is not found in the available channels.
/// </summary>
internal sealed class ChannelNotFoundException : Exception
{
    public ChannelNotFoundException(string message)
        : base(message)
    {
    }

    public ChannelNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
