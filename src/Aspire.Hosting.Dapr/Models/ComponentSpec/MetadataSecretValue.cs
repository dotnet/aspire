// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.Dapr.Models.ComponentSpec;

/// <summary>
/// A metadata value that references a secret
/// </summary>
public sealed class MetadataSecretValue : MetadataValue
{
    /// <summary>
    /// The secret reference
    /// </summary>
    public required SecretKeyRef SecretKeyRef { get; init; }
}

/// <summary>
/// A metadata value that references a secret
/// </summary>
/// <remarks>
/// A metadata value that references a secret
/// </remarks>
/// <param name="Name">The name of the secret defined in the secret store.</param>
/// <param name="Key">The subkey to reference a part of the secret.</param>
public sealed class SecretKeyRef(string Name, string? Key = null)
{
    /// <summary>
    /// The Name of the secret
    /// </summary>
    public string Name { get; init; } = Name;
    /// <summary>
    /// The key of the secret.
    /// If not specified, the Name will be used.
    /// </summary>
    public string Key { get; init; } = Key ?? Name;
}
