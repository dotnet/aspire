// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Provides instances of <see cref="RoundRobinServiceEndPointSelector"/>.
/// </summary>
public class RoundRobinServiceEndPointSelectorProvider : IServiceEndPointSelectorProvider
{
    /// <summary>
    /// Gets a shared instance of this class.
    /// </summary>
    public static RandomServiceEndPointSelectorProvider Instance { get; } = new();

    /// <inheritdoc/>
    public IServiceEndPointSelector CreateSelector() => new RoundRobinServiceEndPointSelector();
}
