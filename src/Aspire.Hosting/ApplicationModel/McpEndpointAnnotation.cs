// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that marks a resource as exposing MCP endpoints.
/// </summary>
internal sealed class McpEndpointAnnotation : IResourceAnnotation
{
    public McpEndpointAnnotation(string transport, EndpointReference endpointReference, string? authToken = null, string? @namespace = null)
    {
        ArgumentNullException.ThrowIfNull(endpointReference);

        Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        EndpointReference = endpointReference;
        AuthToken = authToken;
        Namespace = @namespace;
    }

    public McpEndpointAnnotation(McpEndpointDefinition endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        Transport = endpoint.Transport;
        StaticUri = endpoint.Uri;
        AuthToken = endpoint.AuthToken;
        Namespace = endpoint.Namespace;
    }

    /// <summary>
    /// The transport exposed by the MCP server (e.g. http, websocket, stdio).
    /// </summary>
    public string Transport { get; }

    /// <summary>
    /// Optional bearer token used for authentication.
    /// </summary>
    public string? AuthToken { get; }

    /// <summary>
    /// Optional namespace for tools coming from this endpoint.
    /// </summary>
    public string? Namespace { get; }

    /// <summary>
    /// The endpoint reference (resolved at runtime) if available.
    /// </summary>
    public EndpointReference? EndpointReference { get; }

    /// <summary>
    /// The static URI if the endpoint is already known.
    /// </summary>
    public Uri? StaticUri { get; }

    public static string Serialize(IEnumerable<McpEndpointDefinition> endpoints)
    {
        return JsonSerializer.Serialize(endpoints, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
