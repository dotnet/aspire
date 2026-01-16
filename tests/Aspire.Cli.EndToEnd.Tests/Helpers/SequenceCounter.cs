// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.EndToEnd.Tests.Helpers;

public class SequenceCounter
{
    public int Value { get; private set; } = 1;

    public int Increment()
    {
        return ++Value;
    }
}
