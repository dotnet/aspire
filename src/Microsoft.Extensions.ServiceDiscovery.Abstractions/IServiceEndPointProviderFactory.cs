// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Creates <see cref="IServiceEndPointProvider"/> instances.
/// </summary>
public interface IServiceEndPointProviderFactory
{
    /// <summary>
    /// Tries to create an <see cref="IServiceEndPointProvider"/> instance for the specified <paramref name="query"/>.
    /// </summary>
    /// <param name="query">The service to create the resolver for.</param>
    /// <param name="resolver">The resolver.</param>
    /// <returns><see langword="true"/> if the resolver was created, <see langword="false"/> otherwise.</returns>
    bool TryCreateProvider(ServiceEndPointQuery query, [NotNullWhen(true)] out IServiceEndPointProvider? resolver);
}
