// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Internal;

/// <summary>
/// Service endpoint resolver provider which passes through the provided value.
/// </summary>
internal sealed class PassThroughServiceEndPointResolverProvider : IServiceEndPointResolverProvider
{
    /// <inheritdoc/>
    public bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointResolver? resolver)
    {
        if (!ServiceNameParts.TryCreateEndPoint(serviceName, out var endPoint))
        {
            // Propagate the value through regardless, leaving it to the caller to interpret it.
            endPoint = new DnsEndPoint(serviceName, 0);
        }

        resolver = new PassThroughServiceEndPointResolver(endPoint);
        return true;
    }

    private sealed class PassThroughServiceEndPointResolver(EndPoint endPoint) : IServiceEndPointResolver
    {
        private readonly EndPoint _endPoint = endPoint;

        public ValueTask<ResolutionStatus> ResolveAsync(ServiceEndPointCollectionSource endPoints, CancellationToken cancellationToken)
        {
            if (endPoints.EndPoints.Count != 0)
            {
                return new(ResolutionStatus.None);
            }

            endPoints.EndPoints.Add(ServiceEndPoint.Create(_endPoint));
            return new(ResolutionStatus.Success);
        }

        public ValueTask DisposeAsync() => default;
    }
}
