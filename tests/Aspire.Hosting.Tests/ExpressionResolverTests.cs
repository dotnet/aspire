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
    [InlineData(true, true, "TestEndpoint=http://mytest:1000/stuff;")]
    public async Task ExpressionResolverWorksWithAllCombinationsOfExesAndContainers(bool sourceIsContainer, bool targetIsContainer, string expectedConnectionString)
    {
        var builder = DistributedApplication.CreateBuilder();

        var test = builder.AddResource(new TestContainerResource("mytest"))
            .WithEndpoint("endpoint", e =>
            {
                e.AllocatedEndpoint = new(e, "localhost", 12345, containerHostAddress: "ContainerHostName", targetPortExpression: "1000");
            });

        if (targetIsContainer)
        {
            test = test.WithImage("someimage");
        }

        var csRef = new ConnectionStringReference(test.Resource, false);
        Assert.Equal(expectedConnectionString, await ExpressionResolver.Resolve(sourceIsContainer, csRef, CancellationToken.None));
    }
}

sealed class TestContainerResource(string name) : ContainerResource(name), IResourceWithConnectionString, IResourceWithEndpoints
{
    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"TestEndpoint=http://{MyEndpoint.Property(EndpointProperty.IPV4Host)}:{MyEndpoint.Property(EndpointProperty.Port)}/stuff;");
    public EndpointReference MyEndpoint => new(this, "endpoint");
}
