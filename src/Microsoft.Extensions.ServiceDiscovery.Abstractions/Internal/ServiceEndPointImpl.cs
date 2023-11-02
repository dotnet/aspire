// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

internal sealed class ServiceEndPointImpl : ServiceEndPoint
{
    private readonly IFeatureCollection _features;
    private readonly EndPoint _endPoint;

    public ServiceEndPointImpl(EndPoint endPoint, IFeatureCollection? features = null)
    {
        _endPoint = endPoint;
        _features = features ?? new FeatureCollection();
    }

    public override EndPoint EndPoint => _endPoint;
    public override IFeatureCollection Features => _features;

    public override string? ToString() => GetEndPointString();
}
