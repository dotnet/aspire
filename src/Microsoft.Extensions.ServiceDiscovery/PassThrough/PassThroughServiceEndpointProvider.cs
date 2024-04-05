// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.ServiceDiscovery.PassThrough;

/// <summary>
/// Service endpoint provider which passes through the provided value.
/// </summary>
internal sealed partial class PassThroughServiceEndpointProvider(ILogger logger, string serviceName, EndPoint endPoint) : IServiceEndpointProvider
{
    public ValueTask PopulateAsync(IServiceEndpointBuilder endpoints, CancellationToken cancellationToken)
    {
        if (endpoints.Endpoints.Count == 0)
        {
            Log.UsingPassThrough(logger, serviceName);
            var ep = ServiceEndpoint.Create(endPoint);
            ep.Features.Set<IServiceEndpointProvider>(this);
            endpoints.Endpoints.Add(ep);
        }

        return default;
    }

    public ValueTask DisposeAsync() => default;

    public override string ToString() => "Pass-through";
}
