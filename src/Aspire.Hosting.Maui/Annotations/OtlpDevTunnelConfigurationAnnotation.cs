// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.DevTunnels;
using Aspire.Hosting.Maui.Otlp;

namespace Aspire.Hosting.Maui.Annotations;

/// <summary>
/// Annotation that stores the OTLP dev tunnel configuration for a MAUI project.
/// This allows sharing a single dev tunnel infrastructure across multiple platform resources.
/// </summary>
internal sealed class OtlpDevTunnelConfigurationAnnotation : IResourceAnnotation
{
    /// <summary>
    /// The OTLP loopback stub resource that acts as the service discovery target.
    /// </summary>
    public OtlpLoopbackResource OtlpStub { get; }

    /// <summary>
    /// The resource builder for the OTLP stub (used for WithReference calls).
    /// </summary>
    public IResourceBuilder<OtlpLoopbackResource> OtlpStubBuilder { get; }

    /// <summary>
    /// The dev tunnel resource that tunnels the OTLP endpoint.
    /// </summary>
    public IResourceBuilder<DevTunnelResource> DevTunnel { get; }

    public OtlpDevTunnelConfigurationAnnotation(
        OtlpLoopbackResource otlpStub,
        IResourceBuilder<OtlpLoopbackResource> otlpStubBuilder,
        IResourceBuilder<DevTunnelResource> devTunnel)
    {
        OtlpStub = otlpStub;
        OtlpStubBuilder = otlpStubBuilder;
        DevTunnel = devTunnel;
    }
}
