// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A class that can serialize Aspire service discovery information and save it where the client application can use it.
/// </summary>
public interface IServiceDiscoveryInfoSerializer
{
    /// <summary>
    /// Adds service discovery information for the <paramref name="resource"/> to the application.
    /// </summary>
    /// <param name="resource">The resource for which to add service discovery information.</param>
    void SerializeServiceDiscoveryInfo(IResourceWithEndpoints resource);
}
