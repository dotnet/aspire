// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an MCP endpoint that can be exposed for a resource.
/// </summary>
public sealed record McpEndpointDefinition
{
    /// <summary>
    /// Creates a new <see cref="McpEndpointDefinition"/> instance.
    /// </summary>
    /// <param name="uri">The endpoint URI.</param>
    /// <param name="transport">The transport used by the MCP server (e.g. http, websocket, stdio).</param>
    /// <param name="authToken">Optional bearer token used to authenticate against the MCP server.</param>
    /// <param name="namespace">Optional namespace to group tools from this endpoint.</param>
    [SetsRequiredMembers]
    public McpEndpointDefinition(Uri uri, string transport, string? authToken = null, string? @namespace = null)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        AuthToken = authToken;
        Namespace = @namespace;
    }

    /// <summary>
    /// The MCP endpoint URI (e.g. https://redis-mcp:4000/mcp).
    /// </summary>
    public required Uri Uri { get; init; }

    /// <summary>
    /// The transport used by the MCP server (e.g. http, websocket, stdio).
    /// </summary>
    public required string Transport { get; init; }

    /// <summary>
    /// Optional bearer token used to authenticate against the MCP server.
    /// </summary>
    public string? AuthToken { get; init; }

    /// <summary>
    /// Optional namespace to group tools from this endpoint.
    /// </summary>
    public string? Namespace { get; init; }
}
