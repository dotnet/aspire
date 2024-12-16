// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal sealed class MetadataDirectValue<TValue> : MetadataValue
    where TValue : struct
{
    public required TValue Value { get; init; }
}