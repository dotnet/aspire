// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Dashboard.Model.BrowserStorage;

public readonly struct StorageResult<TValue>
{
    [MemberNotNullWhen(true, nameof(Value))]
    public bool Success { get; }

    public TValue? Value { get; }

    public StorageResult(bool success, TValue? value)
    {
        Success = success;
        Value = value;
    }
}
