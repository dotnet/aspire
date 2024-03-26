// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.Extensions.ServiceDiscovery.Internal;

internal sealed class ServiceEndPointImpl(EndPoint endPoint, IFeatureCollection? features = null) : ServiceEndPoint
{
    public override EndPoint EndPoint { get; } = endPoint;
    public override IFeatureCollection Features { get; } = features ?? new FeatureCollection();
    public override string? ToString() => GetEndPointString();
}
