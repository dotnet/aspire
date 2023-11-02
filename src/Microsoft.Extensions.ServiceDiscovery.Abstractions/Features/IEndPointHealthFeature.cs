// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Represents a feature that reports the health of an endpoint, for use in triggering internal cache refresh and for use in load balancing.
/// </summary>
public interface IEndPointHealthFeature
{
    /// <summary>
    /// Reports health of the endpoint, for use in triggering internal cache refresh and for use in load balancing. Can be a no-op.
    /// </summary>
    /// <param name="responseTime">The response time of the endpoint.</param>
    /// <param name="exception">An optional exception that occurred while checking the endpoint's health.</param>
    void ReportHealth(TimeSpan responseTime, Exception? exception);
}

