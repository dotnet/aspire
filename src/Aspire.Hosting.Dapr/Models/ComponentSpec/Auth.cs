// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.Dapr.Models.ComponentSpec;
/// <summary>
/// Represents the configuration to connect to secret stores
/// </summary>
public sealed class Auth
{
    /// <summary>
    /// name of the secret store component
    /// </summary>
    public required string SecretStore { get; init; }
}