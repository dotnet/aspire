// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Deployment.EndToEnd.Tests.Helpers;

/// <summary>
/// Tracks the sequence number for shell prompt detection in Hex1b terminal sessions.
/// </summary>
public class SequenceCounter
{
    public int Value { get; private set; } = 1;

    public int Increment()
    {
        return ++Value;
    }
}
