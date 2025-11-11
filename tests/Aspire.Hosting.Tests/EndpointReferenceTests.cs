// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace Aspire.Hosting.Tests;

public class EndpointReferenceTests
{
    [Fact]
    public async Task GetValueAsync_WaitsForEndpointAllocation()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);

        var getValueTask = endpointRef.GetValueAsync(CancellationToken.None);
        Assert.False(getValueTask.IsCompleted);

        annotation.AllocatedEndpoint = new AllocatedEndpoint(annotation, "localhost", 8080);

        var url = await getValueTask;
        Assert.Equal("http://localhost:8080", url);
    }

    [Fact]
    public async Task GetUrlPropertyValueAsync_WaitsForEndpointAllocation()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);
        var endpointExpr = endpointRef.Property(EndpointProperty.Url);

        var getValueTask = endpointExpr.GetValueAsync(CancellationToken.None);
        Assert.False(getValueTask.IsCompleted);

        annotation.AllocatedEndpoint = new AllocatedEndpoint(annotation, "localhost", 8080);

        var url = await getValueTask;
        Assert.Equal("http://localhost:8080", url);
    }

    [Fact]
    public async Task GetValueAsync_ReturnsImmediatelyWhenAlreadyAllocated()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);
        annotation.AllocatedEndpoint = new AllocatedEndpoint(annotation, "localhost", 8080);

        var endpointRef = new EndpointReference(resource, annotation);
        var endpointExpr = endpointRef.Property(EndpointProperty.Url);

        var url = await endpointExpr.GetValueAsync(CancellationToken.None);
        Assert.Equal("http://localhost:8080", url);
    }

    [Fact]
    public async Task GetValueAsync_Host_WaitsForAllocation()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);
        var hostExpr = endpointRef.Property(EndpointProperty.Host);

        var getValueTask = hostExpr.GetValueAsync(CancellationToken.None);
        Assert.False(getValueTask.IsCompleted);

        annotation.AllocatedEndpoint = new AllocatedEndpoint(annotation, "192.168.1.100", 8080);

        var host = await getValueTask;
        Assert.Equal("192.168.1.100", host);
    }

    [Fact]
    public async Task GetValueAsync_Port_WaitsForAllocation()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);
        var portExpr = endpointRef.Property(EndpointProperty.Port);

        var getValueTask = portExpr.GetValueAsync(CancellationToken.None);
        Assert.False(getValueTask.IsCompleted);

        annotation.AllocatedEndpoint = new AllocatedEndpoint(annotation, "localhost", 9090);

        var port = await getValueTask;
        Assert.Equal("9090", port);
    }

    [Fact]
    public async Task GetValueAsync_Scheme_ReturnsImmediately()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "https", name: "https");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);
        var schemeExpr = endpointRef.Property(EndpointProperty.Scheme);

        var scheme = await schemeExpr.GetValueAsync(CancellationToken.None);
        Assert.Equal("https", scheme);
    }

    [Fact]
    public async Task GetValueAsync_IPV4Host_ReturnsImmediately()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);
        var ipv4Expr = endpointRef.Property(EndpointProperty.IPV4Host);

        var ipv4 = await ipv4Expr.GetValueAsync(CancellationToken.None);
        Assert.Equal("127.0.0.1", ipv4);
    }

    [Fact]
    public async Task GetValueAsync_TargetPort_WithStaticPort_ReturnsImmediately()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http", targetPort: 5000);
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);
        var targetPortExpr = endpointRef.Property(EndpointProperty.TargetPort);

        var targetPort = await targetPortExpr.GetValueAsync(CancellationToken.None);
        Assert.Equal("5000", targetPort);
    }

    [Fact]
    public async Task GetValueAsync_TargetPort_WithExpression_WaitsForAllocation()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);
        var targetPortExpr = endpointRef.Property(EndpointProperty.TargetPort);

        var getValueTask = targetPortExpr.GetValueAsync(CancellationToken.None);
        Assert.False(getValueTask.IsCompleted);

        annotation.AllocatedEndpoint = new AllocatedEndpoint(annotation, "localhost", 8080, targetPortExpression: "5000");

        var targetPort = await getValueTask;
        Assert.Equal("5000", targetPort);
    }

    [Fact]
    public async Task GetValueAsync_SupportsCancellation()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);
        var endpointExpr = endpointRef.Property(EndpointProperty.Url);

        using var cts = new CancellationTokenSource();
        var getValueTask = endpointExpr.GetValueAsync(cts.Token);

        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await getValueTask);
    }

    [Fact]
    public async Task GetValueAsync_MultipleWaiters_AllCompleteWhenAllocated()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);
        var expr1 = endpointRef.Property(EndpointProperty.Url);
        var expr2 = endpointRef.Property(EndpointProperty.Host);
        var expr3 = endpointRef.Property(EndpointProperty.Port);

        var task1 = expr1.GetValueAsync(CancellationToken.None);
        var task2 = expr2.GetValueAsync(CancellationToken.None);
        var task3 = expr3.GetValueAsync(CancellationToken.None);

        Assert.False(task1.IsCompleted);
        Assert.False(task2.IsCompleted);
        Assert.False(task3.IsCompleted);

        annotation.AllocatedEndpoint = new AllocatedEndpoint(annotation, "localhost", 8080);

        var url = await task1;
        var host = await task2;
        var port = await task3;

        Assert.Equal("http://localhost:8080", url);
        Assert.Equal("localhost", host);
        Assert.Equal("8080", port);
    }

    [Fact]
    public void Port_ThrowsWhenEndpointNotAllocated()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);

        var ex = Assert.Throws<InvalidOperationException>(() => endpointRef.Port);
        Assert.Equal("The endpoint `http` is not allocated for the resource `test`.", ex.Message);
    }

    [Fact]
    public void Host_ThrowsWhenEndpointNotAllocated()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);

        var ex = Assert.Throws<InvalidOperationException>(() => endpointRef.Host);
        Assert.Equal("The endpoint `http` is not allocated for the resource `test`.", ex.Message);
    }

    [Fact]
    public void Url_ThrowsWhenEndpointNotAllocated()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);

        var ex = Assert.Throws<InvalidOperationException>(() => endpointRef.Url);
        Assert.Equal("The endpoint `http` is not allocated for the resource `test`.", ex.Message);
    }

    [Fact]
    public void Scheme_DoesNotThrowWhenEndpointNotAllocated()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "https", name: "https");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);

        var scheme = endpointRef.Scheme;
        Assert.Equal("https", scheme);
    }

    [Fact]
    public void TargetPort_DoesNotThrowWhenStaticPortDefined()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http", targetPort: 5000);
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);

        var targetPort = endpointRef.TargetPort;
        Assert.Equal(5000, targetPort);
    }

    [Fact]
    public void TargetPort_ReturnsNullWhenNotDefined()
    {
        var resource = new TestResource("test");
        var annotation = new EndpointAnnotation(ProtocolType.Tcp, uriScheme: "http", name: "http");
        resource.Annotations.Add(annotation);

        var endpointRef = new EndpointReference(resource, annotation);

        var targetPort = endpointRef.TargetPort;
        Assert.Null(targetPort);
    }

    private sealed class TestResource(string name) : Resource(name), IResourceWithEndpoints
    {
    }
}
