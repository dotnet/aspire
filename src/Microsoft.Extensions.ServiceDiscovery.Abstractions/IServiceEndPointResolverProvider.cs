// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Creates <see cref="IServiceEndPointResolver"/> instances.
/// </summary>
public interface IServiceEndPointResolverProvider
{
    /// <summary>
    /// Tries to create an <see cref="IServiceEndPointResolver"/> instance for the specified <paramref name="serviceName"/>.
    /// </summary>
    /// <param name="serviceName">The service to create the resolver for.</param>
    /// <param name="resolver">The resolver.</param>
    /// <returns><see langword="true"/> if the resolver was created, <see langword="false"/> otherwise.</returns>
    bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointResolver? resolver);
}
