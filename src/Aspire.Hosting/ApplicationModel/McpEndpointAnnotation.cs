// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that identifies an endpoint on a resource that hosts a Model Context Protocol (MCP) server.
/// </summary>
/// <remarks>
/// This annotation is intended for discovery and proxying scenarios where the Aspire AppHost can act as a mediator
/// between clients (such as the Aspire CLI) and MCP servers exposed by resources.
/// </remarks>
public sealed class McpEndpointAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpEndpointAnnotation"/> class.
    /// </summary>
    /// <param name="endpointName">The name of the endpoint on the resource that hosts the MCP server.</param>
    /// <param name="path">An optional path to append to the endpoint URL. Defaults to <c>"/mcp"</c>.</param>
    public McpEndpointAnnotation(string endpointName, string? path = "/mcp")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);

        EndpointName = endpointName;
        Path = path;
    }

    /// <summary>
    /// Gets the name of the endpoint on the resource that hosts the MCP server.
    /// </summary>
    public string EndpointName { get; }

    /// <summary>
    /// Gets the optional path to append to the endpoint URL.
    /// </summary>
    public string? Path { get; }
}
