// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Tests.Model;

public sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
{
    public TestOptionsMonitor(T currentValue)
    {
        CurrentValue = currentValue;
    }

    public T CurrentValue { get; }

    public T Get(string? name)
    {
        throw new NotImplementedException();
    }

    public IDisposable? OnChange(Action<T, string?> listener)
    {
        throw new NotImplementedException();
    }
}
