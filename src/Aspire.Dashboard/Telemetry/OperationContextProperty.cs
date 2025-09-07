// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Telemetry;

[DebuggerDisplay("HasValue = {HasValue}, Value = {_value}")]
public sealed class OperationContextProperty
{
    private object? _value;

    public bool HasValue { get; private set; }

    public object GetValue()
    {
        if (!HasValue)
        {
            throw new InvalidOperationException("Value has not been set.");
        }
        return _value!;
    }

    public void SetValue(object value)
    {
        if (HasValue)
        {
            throw new InvalidOperationException("Value has already been set.");
        }
        _value = value;
        HasValue = true;
    }
}
