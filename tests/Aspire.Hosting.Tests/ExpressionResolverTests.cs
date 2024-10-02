// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;
public class ExpressionResolverTests
{
    [Theory]
    [InlineData(typeof(TestContainerResource), false, false, "TestEndpoint=http://127.0.0.1:12345/stuff;")]
    [InlineData(typeof(TestContainerResource), false, true, "TestEndpoint=http://127.0.0.1:12345/stuff;")]
    [InlineData(typeof(TestContainerResource), true, false, "TestEndpoint=http://ContainerHostName:12345/stuff;")]
    [InlineData(typeof(TestContainerResource), true, true, "TestEndpoint=http://testresource:10000/stuff;")]
    [InlineData(typeof(TestContainerResourceWithUrlExpression), false, false, "Url=http://localhost:12345;")]
    [InlineData(typeof(TestContainerResourceWithUrlExpression), false, true, "Url=http://localhost:12345;")]
    [InlineData(typeof(TestContainerResourceWithUrlExpression), true, false, "Url=http://ContainerHostName:12345;")]
    [InlineData(typeof(TestContainerResourceWithUrlExpression), true, true, "Url=http://testresource:10000;")]
    [InlineData(typeof(TestContainerResourceWithOnlyHost), true, false, "Host=ContainerHostName;")]
    [InlineData(typeof(TestContainerResourceWithOnlyHost), true, true, "Host=testresource;")]
    [InlineData(typeof(TestContainerResourceWithOnlyPort), true, false, "Port=12345;")]
    [InlineData(typeof(TestContainerResourceWithOnlyPort), true, true, "Port=12345;")]
    [InlineData(typeof(TestContainerResourceWithPortBeforeHost), true, false, "Port=12345;Host=ContainerHostName;")]
    //[InlineData(typeof(TestContainerResourceWithPortBeforeHost), true, true, "Port=10000;Host=testresource;")]    //TODO: enable when fixed
    public async Task ExpressionResolverGeneratesCorrectStrings(Type t, bool sourceIsContainer, bool targetIsContainer, string expectedConnectionString)
    {
        var builder = DistributedApplication.CreateBuilder();

        var r = (BaseTestContainerResource)Activator.CreateInstance(t)!;

        var test = builder.AddResource(r)
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

abstract class BaseTestContainerResource : ContainerResource, IResourceWithEndpoints, IResourceWithConnectionString
{
protected EndpointReference MyEndpoint { get; }
    public BaseTestContainerResource() : base("testresource")
    {
        MyEndpoint = new(this, "endpoint");
    }

    public abstract ReferenceExpression ConnectionStringExpression { get; }
}

sealed class TestContainerResource : BaseTestContainerResource
{
    public override ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"TestEndpoint=http://{MyEndpoint.Property(EndpointProperty.IPV4Host)}:{MyEndpoint.Property(EndpointProperty.Port)}/stuff;");
}

sealed class TestContainerResourceWithUrlExpression : BaseTestContainerResource
{
    public override ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"Url={MyEndpoint.Property(EndpointProperty.Url)};");
}

sealed class TestContainerResourceWithOnlyHost : BaseTestContainerResource
{
    public override ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"Host={MyEndpoint.Property(EndpointProperty.Host)};");
}

sealed class TestContainerResourceWithOnlyPort : BaseTestContainerResource
{
    public override ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"Port={MyEndpoint.Property(EndpointProperty.Port)};");
}

sealed class TestContainerResourceWithPortBeforeHost : BaseTestContainerResource
{
    public override ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"Port={MyEndpoint.Property(EndpointProperty.Port)};Host={MyEndpoint.Property(EndpointProperty.Host)};");
}

