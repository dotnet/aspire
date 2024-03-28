// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that provides an OTLP server
/// </summary>
public class OtlpServerResourceAnnotation : IResourceAnnotation
{
    /// <summary>
    /// 
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? GrpcUrl { get; set; }

    public EndpointReference? GrpcEndpoint { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? HttpEndpoint { get; set; }
}
