// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an identifier for a container network.
/// </summary>
/// <param name="Value">The string value of the network identifier.</param>
public readonly record struct ContainerNetworkIdentifier(string Value)
{
    /// <summary>
    /// Gets the default Aspire container network identifier.
    /// </summary>
    public static ContainerNetworkIdentifier Default { get; } = new("aspire-network");

    /// <inheritdoc />
    public override string ToString() => Value;
}
