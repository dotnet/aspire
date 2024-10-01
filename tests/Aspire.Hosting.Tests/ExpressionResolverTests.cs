// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;
public class ExpressionResolverTests
{
    [Theory]
    [InlineData(false, false, "TestEndpoint=http://127.0.0.1:12345/stuff;")]
    [InlineData(false, true, "TestEndpoint=http://127.0.0.1:12345/stuff;")]
    [InlineData(true, false, "TestEndpoint=http://ContainerHostName:12345/stuff;")]
    [InlineData(true, true, "TestEndpoint=http://mytest:10000/stuff;")]
    public async Task ExpressionResolverWorksWithAllCombinationsOfExesAndContainers(bool sourceIsContainer, bool targetIsContainer, string expectedConnectionString)
    {
        var builder = DistributedApplication.CreateBuilder();

        var test = builder.AddResource(new TestContainerResource("mytest"))
            .WithEndpoint("endpoint", e =>
            {
                e.UriScheme = "http";
                e.AllocatedEndpoint = new(e, "localhost", 12345, containerHostAddress: "ContainerHostName", targetPortExpression: "10000");
            });

        if (targetIsContainer)
        {
            test = test.WithImage("someimage");
        }

        var csRef = new ConnectionStringReference(test.Resource, false);
        var connectionString = await ExpressionResolver.Resolve(sourceIsContainer, csRef, CancellationToken.None);
        Assert.Equal(expectedConnectionString, connectionString);
    }

    [Theory]
    [InlineData(false, false, "Url=http://localhost:12345;")]
    [InlineData(false, true, "Url=http://localhost:12345;")]
    [InlineData(true, false, "Url=http://ContainerHostName:12345;")]
    [InlineData(true, true, "Url=http://mytest:10000;")]
    public async Task ExpressionResolverWorksWithAllCombinationsOfExesAndContainersUrlProp(bool sourceIsContainer, bool targetIsContainer, string expectedConnectionString)
    {
        var builder = DistributedApplication.CreateBuilder();

        var test = builder.AddResource(new TestContainerResourceWithUrlExpression("mytest"))
            .WithEndpoint("endpoint", e =>
            {
                e.UriScheme = "http";
                e.AllocatedEndpoint = new(e, "localhost", 12345, containerHostAddress: "ContainerHostName", targetPortExpression: "10000");
            });

        if (targetIsContainer)
        {
            test = test.WithImage("someimage");
        }

        var csRef = new ConnectionStringReference(test.Resource, false);
        var connectionString = await ExpressionResolver.Resolve(sourceIsContainer, csRef, CancellationToken.None);
        Assert.Equal(expectedConnectionString, connectionString);
    }
}

sealed class TestContainerResource : ContainerResource, IResourceWithConnectionString, IResourceWithEndpoints
{
    private readonly EndpointReference _myEndpoint;
    public TestContainerResource(string name) : base(name)
    {
        _myEndpoint = new(this, "endpoint");
    }

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"TestEndpoint=http://{_myEndpoint.Property(EndpointProperty.IPV4Host)}:{_myEndpoint.Property(EndpointProperty.Port)}/stuff;");
}

sealed class TestContainerResourceWithUrlExpression : ContainerResource, IResourceWithConnectionString, IResourceWithEndpoints
{
    private readonly EndpointReference _myEndpoint;
    public TestContainerResourceWithUrlExpression(string name) : base(name)
    {
        _myEndpoint = new(this, "endpoint");
    }

    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"Url={_myEndpoint.Property(EndpointProperty.Url)};");
}
