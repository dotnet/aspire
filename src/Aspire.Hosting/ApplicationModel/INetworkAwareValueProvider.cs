// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Value provider that can resolve values in the context of a specific communication network.
/// </summary>
public interface INetworkAwareValueProvider : IValueProvider
{
    /// <summary>
    /// Gets the value in the context of a specific communication network.
    /// </summary>
    /// <param name="context">The identifier for the network that servers as context for value retrieval. If not specified (null), the value provider will assume its default network as the context.</param>
    /// <param name="cancellationToken">The cancellation token for the value retrieval operation.</param>
    public ValueTask<string?> GetValueAsync(NetworkIdentifier? context, CancellationToken cancellationToken = default);
}
