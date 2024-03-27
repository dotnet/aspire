// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.ServiceDiscovery.PassThrough;

/// <summary>
/// Service endpoint resolver which passes through the provided value.
/// </summary>
internal sealed partial class PassThroughServiceEndPointResolver(ILogger logger, string serviceName, EndPoint endPoint) : IServiceEndPointProvider
{
    public ValueTask PopulateAsync(IServiceEndPointBuilder endPoints, CancellationToken cancellationToken)
    {
        if (endPoints.EndPoints.Count == 0)
        {
            Log.UsingPassThrough(logger, serviceName);
            var ep = ServiceEndPoint.Create(endPoint);
            ep.Features.Set<IServiceEndPointProvider>(this);
            endPoints.EndPoints.Add(ep);
        }

        return default;
    }

    public ValueTask DisposeAsync() => default;

    public override string ToString() => "Pass-through";
}
