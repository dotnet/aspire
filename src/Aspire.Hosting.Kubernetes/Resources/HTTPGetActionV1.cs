// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using YamlDotNet.Serialization;

namespace Aspire.Hosting.Kubernetes.Resources;

/// <summary>
/// HTTPGetActionV1 represents an HTTP GET request action in Kubernetes resources.
/// </summary>
/// <remarks>
/// This action is typically used within Kubernetes probes or handlers to perform HTTP GET requests
/// for purposes such as health checks or event triggering. A URL is constructed using the specified
/// scheme, host, port, and path, and optional HTTP headers can also be included in the request.
/// </remarks>
[YamlSerializable]
public sealed class HttpGetActionV1
{
    /// <summary>
    /// Gets or sets the scheme to use for the HTTP request.
    /// This property determines whether the request is sent using "HTTP" or "HTTPS".
    /// </summary>
    [YamlMember(Alias = "scheme")]
    public string Scheme { get; set; } = null!;

    /// <summary>
    /// Gets or sets the relative path for the HTTP request.
    /// The path specifies the endpoint to be accessed on the server.
    /// </summary>
    [YamlMember(Alias = "path")]
    public string Path { get; set; } = null!;

    /// <summary>
    /// Represents a collection of HTTP headers that can be added to an HTTP request
    /// in the context of a Kubernetes HTTPGetActionV1 resource.
    /// </summary>
    /// <remarks>
    /// This property provides a list of HTTPHeaderV1 objects, where each object
    /// specifies the name and value of an HTTP header. These headers will be
    /// included in the HTTP request made by the Kubernetes resource.
    /// </remarks>
    [YamlMember(Alias = "httpHeaders")]
    public List<HttpHeaderV1> HttpHeaders { get; } = [];

    /// <summary>
    /// Gets or sets the hostname to use for the HTTP GET request.
    /// This specifies the DNS name or IP address of the server to connect to.
    /// </summary>
    [YamlMember(Alias = "host")]
    public string Host { get; set; } = null!;

    /// <summary>
    /// Gets or sets the port number on which the HTTP request will be sent.
    /// </summary>
    [YamlMember(Alias = "port")]
    public int Port { get; set; }
}
