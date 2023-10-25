// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Exposes the host name of the end point.
/// </summary>
public interface IHostNameFeature
{
    /// <summary>
    /// Gets the host name of the end point.
    /// </summary>
    public string HostName { get; }
}

