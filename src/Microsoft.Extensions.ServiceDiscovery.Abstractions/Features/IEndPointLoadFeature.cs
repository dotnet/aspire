// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Represents a feature that provides information about the current load of an endpoint.
/// </summary>
public interface IEndPointLoadFeature
{
    /// <summary>
    /// Gets a comparable measure of the current load of the endpoint (e.g. queue length, concurrent requests, etc).
    /// </summary>
    public double CurrentLoad { get; }
}

