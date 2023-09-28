// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Functionality for creating <see cref="IServiceEndPointSelector"/> instances.
/// </summary>
public interface IServiceEndPointSelectorProvider
{
    /// <summary>
    /// Creates an <see cref="IServiceEndPointSelector"/> instance.
    /// </summary>
    /// <returns>A new <see cref="IServiceEndPointSelector"/> instance.</returns>
    IServiceEndPointSelector CreateSelector();
}
