// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.Dapr.Models.ComponentSpec;
/// <summary>
/// A metadata value that defines a direct value
/// </summary>
/// <typeparam name="TValue">The type of the vale</typeparam>
public sealed class MetadataDirectValue<TValue> : MetadataValue
{
    /// <summary>
    /// The value of the metadata
    /// </summary>
    public required TValue Value { get; init; }
}