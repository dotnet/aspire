// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace Aspire.Hosting.Tests.ApplicationModel;

public class McpEndpointAnnotationTests
{
    [Fact]
    public void Constructor_WithEndpointReference_InitializesProperties()
    {
        var resource = new TestResource("test");
        var endpointAnnotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http", targetPort: 5000);
        resource.Annotations.Add(endpointAnnotation);
        var endpointReference = new EndpointReference(resource, endpointAnnotation);
        var transport = "http";
        var authToken = "test-token";
        var @namespace = "test-namespace";

        var annotation = new McpEndpointAnnotation(transport, endpointReference, authToken, @namespace);

        Assert.Equal(transport, annotation.Transport);
        Assert.Equal(endpointReference, annotation.EndpointReference);
        Assert.Equal(authToken, annotation.AuthToken);
        Assert.Equal(@namespace, annotation.Namespace);
        Assert.Null(annotation.StaticUri);
    }

    [Fact]
    public void Constructor_WithEndpointReference_ThrowsWhenEndpointReferenceIsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new McpEndpointAnnotation("http", null!, "token", "namespace"));

        Assert.Equal("endpointReference", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithEndpointReference_ThrowsWhenTransportIsNull()
    {
        var resource = new TestResource("test");
        var endpointAnnotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(endpointAnnotation);
        var endpointReference = new EndpointReference(resource, endpointAnnotation);

        var ex = Assert.Throws<ArgumentNullException>(() =>
            new McpEndpointAnnotation(null!, endpointReference, "token", "namespace"));

        Assert.Equal("transport", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithEndpointReference_AllowsNullAuthToken()
    {
        var resource = new TestResource("test");
        var endpointAnnotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(endpointAnnotation);
        var endpointReference = new EndpointReference(resource, endpointAnnotation);

        var annotation = new McpEndpointAnnotation("http", endpointReference, authToken: null, @namespace: "test");

        Assert.Null(annotation.AuthToken);
        Assert.Equal("test", annotation.Namespace);
    }

    [Fact]
    public void Constructor_WithEndpointReference_AllowsNullNamespace()
    {
        var resource = new TestResource("test");
        var endpointAnnotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(endpointAnnotation);
        var endpointReference = new EndpointReference(resource, endpointAnnotation);

        var annotation = new McpEndpointAnnotation("http", endpointReference, authToken: "token", @namespace: null);

        Assert.Equal("token", annotation.AuthToken);
        Assert.Null(annotation.Namespace);
    }

    [Fact]
    public void Constructor_WithMcpEndpointDefinition_InitializesProperties()
    {
        var uri = new Uri("http://localhost:5000/mcp");
        var transport = "http";
        var authToken = "test-token";
        var @namespace = "test-namespace";
        var definition = new McpEndpointDefinition(uri, transport, authToken, @namespace);

        var annotation = new McpEndpointAnnotation(definition);

        Assert.Equal(transport, annotation.Transport);
        Assert.Equal(uri, annotation.StaticUri);
        Assert.Equal(authToken, annotation.AuthToken);
        Assert.Equal(@namespace, annotation.Namespace);
        Assert.Null(annotation.EndpointReference);
    }

    [Fact]
    public void Constructor_WithMcpEndpointDefinition_ThrowsWhenDefinitionIsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new McpEndpointAnnotation((McpEndpointDefinition)null!));

        Assert.Equal("endpoint", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithMcpEndpointDefinition_HandlesNullAuthToken()
    {
        var uri = new Uri("http://localhost:5000/mcp");
        var definition = new McpEndpointDefinition(uri, "http", authToken: null, @namespace: "test");

        var annotation = new McpEndpointAnnotation(definition);

        Assert.Null(annotation.AuthToken);
        Assert.Equal("test", annotation.Namespace);
    }

    [Fact]
    public void Constructor_WithMcpEndpointDefinition_HandlesNullNamespace()
    {
        var uri = new Uri("http://localhost:5000/mcp");
        var definition = new McpEndpointDefinition(uri, "http", authToken: "token", @namespace: null);

        var annotation = new McpEndpointAnnotation(definition);

        Assert.Equal("token", annotation.AuthToken);
        Assert.Null(annotation.Namespace);
    }

    [Fact]
    public void Serialize_ReturnsJsonStringForSingleEndpoint()
    {
        var uri = new Uri("http://localhost:5000/mcp");
        var definition = new McpEndpointDefinition(uri, "http", "token", "namespace");
        var endpoints = new[] { definition };

        var json = McpEndpointAnnotation.Serialize(endpoints);

        Assert.NotNull(json);
        Assert.Contains("\"uri\"", json);
        Assert.Contains("\"transport\"", json);
        Assert.Contains("\"authToken\"", json);
        Assert.Contains("\"namespace\"", json);
        Assert.Contains("http://localhost:5000/mcp", json);
        Assert.Contains("http", json);
        Assert.Contains("token", json);
        Assert.Contains("namespace", json);
    }

    [Fact]
    public void Serialize_ReturnsJsonStringForMultipleEndpoints()
    {
        var definition1 = new McpEndpointDefinition(new Uri("http://localhost:5000/mcp"), "http", "token1", "ns1");
        var definition2 = new McpEndpointDefinition(new Uri("http://localhost:6000/mcp"), "websocket", "token2", "ns2");
        var endpoints = new[] { definition1, definition2 };

        var json = McpEndpointAnnotation.Serialize(endpoints);

        Assert.NotNull(json);
        Assert.Contains("http://localhost:5000/mcp", json);
        Assert.Contains("http://localhost:6000/mcp", json);
        Assert.Contains("websocket", json);
        Assert.Contains("token1", json);
        Assert.Contains("token2", json);
        Assert.Contains("ns1", json);
        Assert.Contains("ns2", json);
    }

    [Fact]
    public void Serialize_HandlesEmptyCollection()
    {
        var endpoints = Array.Empty<McpEndpointDefinition>();

        var json = McpEndpointAnnotation.Serialize(endpoints);

        Assert.NotNull(json);
        Assert.Equal("[]", json);
    }

    [Fact]
    public void Serialize_UsesCamelCaseNaming()
    {
        var uri = new Uri("http://localhost:5000/mcp");
        var definition = new McpEndpointDefinition(uri, "http", "token", "namespace");
        var endpoints = new[] { definition };

        var json = McpEndpointAnnotation.Serialize(endpoints);

        // Verify camelCase is used
        Assert.Contains("\"uri\"", json);
        Assert.Contains("\"transport\"", json);
        Assert.Contains("\"authToken\"", json);
        Assert.Contains("\"namespace\"", json);
        
        // Verify PascalCase is NOT used
        Assert.DoesNotContain("\"Uri\"", json);
        Assert.DoesNotContain("\"Transport\"", json);
        Assert.DoesNotContain("\"AuthToken\"", json);
        Assert.DoesNotContain("\"Namespace\"", json);
    }

    [Fact]
    public void AnnotationWithEndpointReference_HasNoStaticUri()
    {
        var resource = new TestResource("test");
        var endpointAnnotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(endpointAnnotation);
        var endpointReference = new EndpointReference(resource, endpointAnnotation);

        var annotation = new McpEndpointAnnotation("http", endpointReference, "token", "namespace");

        Assert.NotNull(annotation.EndpointReference);
        Assert.Null(annotation.StaticUri);
    }

    [Fact]
    public void AnnotationWithStaticUri_HasNoEndpointReference()
    {
        var uri = new Uri("http://localhost:5000/mcp");
        var definition = new McpEndpointDefinition(uri, "http", "token", "namespace");

        var annotation = new McpEndpointAnnotation(definition);

        Assert.Null(annotation.EndpointReference);
        Assert.NotNull(annotation.StaticUri);
    }

    [Theory]
    [InlineData("http")]
    [InlineData("websocket")]
    [InlineData("stdio")]
    [InlineData("custom-transport")]
    public void Constructor_AcceptsDifferentTransportTypes(string transport)
    {
        var uri = new Uri("http://localhost:5000/mcp");
        var definition = new McpEndpointDefinition(uri, transport);

        var annotation = new McpEndpointAnnotation(definition);

        Assert.Equal(transport, annotation.Transport);
    }

    [Fact]
    public void McpEndpointAnnotation_ImplementsIResourceAnnotation()
    {
        var uri = new Uri("http://localhost:5000/mcp");
        var definition = new McpEndpointDefinition(uri, "http");

        var annotation = new McpEndpointAnnotation(definition);

        Assert.IsAssignableFrom<IResourceAnnotation>(annotation);
    }

    private sealed class TestResource(string name) : Resource(name), IResourceWithEndpoints
    {
    }
}
