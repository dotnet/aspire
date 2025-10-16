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
    public ValueTask<string?> GetValueAsync(NetworkIdentifier? context = null, CancellationToken cancellationToken = default);
}
