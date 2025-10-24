// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

/// <summary>
/// A value provider that will resolve to a specific value at runtime, but does not provide any
/// guarantees about contents of the value.
/// </summary>
public record OpaqueValueProvider : IValueProvider
{
    private string? _value;

    /// <inheritdoc/>
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_value);
    }

    internal void SetValue(string value)
    {
        _value = value;
    }
}