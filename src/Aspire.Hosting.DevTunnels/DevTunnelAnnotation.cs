// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.DevTunnels;

internal class DevTunnelAnnotation(EndpointAnnotation endpointAnnotation, string defaultDevTunnelId) : IResourceAnnotation
{
    public EndpointAnnotation EndpointAnnotation { get; } = endpointAnnotation;
    public DevTunnelOptions Options { get; } = new(defaultDevTunnelId);
}
