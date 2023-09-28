// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

public interface IEndPointHealthFeature
{
    // Reports health of the endpoint, for use in triggering internal cache refresh and for use in load balancing.
    // Can be a no-op.
    void ReportHealth(TimeSpan responseTime, Exception? exception);
}

