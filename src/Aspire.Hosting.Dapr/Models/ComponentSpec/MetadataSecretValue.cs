// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.Dapr.Models.ComponentSpec;

/// <summary>
/// A metadata value that references a secret
/// </summary>
public sealed class MetadataSecretValue: MetadataValue
{
    /// <summary>
    /// The name of the secret
    /// </summary>
    public required string SecretName { get; init; }
    /// <summary>
    /// The key of the secret
    /// </summary>
    public required string SecretKey { get; init; }
}