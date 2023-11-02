// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.PassThrough;

/// <summary>
/// Service endpoint resolver which passes through the provided value.
/// </summary>
internal sealed partial class PassThroughServiceEndPointResolver(ILogger logger, string serviceName, EndPoint endPoint) : IServiceEndPointResolver
{
    public string DisplayName => "Pass-through";

    public ValueTask<ResolutionStatus> ResolveAsync(ServiceEndPointCollectionSource endPoints, CancellationToken cancellationToken)
    {
        if (endPoints.EndPoints.Count != 0)
        {
            return new(ResolutionStatus.None);
        }

        Log.UsingPassThrough(logger, serviceName);
        var ep = ServiceEndPoint.Create(endPoint);
        ep.Features.Set<IServiceEndPointResolver>(this);
        endPoints.EndPoints.Add(ep);
        return new(ResolutionStatus.Success);
    }

    public ValueTask DisposeAsync() => default;
}
