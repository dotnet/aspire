// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui.DevTunnels;

/// <summary>
/// Synthetic hidden resource exposing the local collector OTLP port so it can be tunneled to devices.
/// </summary>
internal sealed class OtlpLoopbackResource : Resource, IResourceWithEndpoints
{
    public OtlpLoopbackResource(string name, int port, string scheme) : base(name)
    {
        if (port <= 0 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }
        if (string.IsNullOrWhiteSpace(scheme))
        {
            scheme = "http";
        }
        // Stable endpoint name 'otlp' so service discovery key is services__{stubName}__otlp__0 regardless of scheme.
        Annotations.Add(new EndpointAnnotation(System.Net.Sockets.ProtocolType.Tcp, uriScheme: scheme, name: "otlp", port: port, isProxied: false)
        {
            TargetHost = "localhost"
        });
    }
}
