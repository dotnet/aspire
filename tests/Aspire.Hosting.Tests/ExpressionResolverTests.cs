// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ExpressionResolverTests
{
    [Theory]
    [MemberData(nameof(ResolveInternalAsync_ResolvesCorrectly_MemberData))]
    public async Task ResolveInternalAsync_ResolvesCorrectly(ExpressionResolverTestData testData, Type? exceptionType, (string Value, bool IsSensitive)? expectedValue)
    {
        if (exceptionType is not null)
        {
            await Assert.ThrowsAsync(exceptionType, ResolveAsync);
        }
        else
        {
            var resolvedValue = await ExpressionResolver.ResolveAsync(testData.SourceIsContainer, testData.ValueProvider, string.Empty, CancellationToken.None);

            Assert.Equal(expectedValue?.Value, resolvedValue.Value);
            Assert.Equal(expectedValue?.IsSensitive, resolvedValue.IsSensitive);
        }

        async Task<ResolvedValue> ResolveAsync() => await ExpressionResolver.ResolveAsync(testData.SourceIsContainer, testData.ValueProvider, string.Empty, CancellationToken.None);
    }

    public static TheoryData<ExpressionResolverTestData, Type?, (string? Value, bool IsSensitive)?> ResolveInternalAsync_ResolvesCorrectly_MemberData()
    {
        var data = new TheoryData<ExpressionResolverTestData, Type?, (string? Value, bool IsSensitive)?>();

        // doesn't differ by sourceIsContainer
        data.Add(new ExpressionResolverTestData(false, new ConnectionStringReference(new TestExpressionResolverResource("Empty"), false)), typeof(DistributedApplicationException), null);
        data.Add(new ExpressionResolverTestData(false, new ConnectionStringReference(new TestExpressionResolverResource("Empty"), true)), null, (null, false));
        data.Add(new ExpressionResolverTestData(true, new ConnectionStringReference(new TestExpressionResolverResource("String"), true)), null, ("String", false));
        data.Add(new ExpressionResolverTestData(true, new ConnectionStringReference(new TestExpressionResolverResource("SecretParameter"), false)), null, ("SecretParameter", true));

        // IResourceWithConnectionString resolves differently for ConnectionStringParameterResource (as a secret parameter)
        data.Add(new ExpressionResolverTestData(false, new ConnectionStringParameterResource("SurrogateResource", _ => "SurrogateResource", null)), null, ("SurrogateResource", true));
        data.Add(new ExpressionResolverTestData(false, new TestExpressionResolverResource("String")), null, ("String", false));

        data.Add(new ExpressionResolverTestData(false, new ParameterResource("SecretParameter", _ => "SecretParameter", secret: true)), null, ("SecretParameter", true));
        data.Add(new ExpressionResolverTestData(false, new ParameterResource("NonSecretParameter", _ => "NonSecretParameter", secret: false)), null, ("NonSecretParameter", false));

        // ExpressionResolverGeneratesCorrectEndpointStrings separately tests EndpointReference and EndpointReferenceExpression

        return data;
    }

    public record ExpressionResolverTestData(bool SourceIsContainer, IValueProvider ValueProvider);

    [Theory]
    [InlineData("TwoFullEndpoints", false, false, "Test1=http://127.0.0.1:12345/;Test2=https://localhost:12346/;")]
    [InlineData("TwoFullEndpoints", false, true, "Test1=http://127.0.0.1:12345/;Test2=https://localhost:12346/;")]
    [InlineData("TwoFullEndpoints", true, false, "Test1=http://ContainerHostName:12345/;Test2=https://ContainerHostName:12346/;")]
    [InlineData("TwoFullEndpoints", true, true, "Test1=http://testresource:10000/;Test2=https://testresource:10001/;")]
    [InlineData("Url", false, false, "Url=http://localhost:12345;")]
    [InlineData("Url", false, true, "Url=http://localhost:12345;")]
    [InlineData("Url", true, false, "Url=http://ContainerHostName:12345;")]
    [InlineData("Url", true, true, "Url=http://testresource:10000;")]
    [InlineData("Url2", true, false, "Url=http://ContainerHostName:12345;")]
    [InlineData("Url2", true, true, "Url=http://testresource:10000;")]
    [InlineData("OnlyHost", true, false, "Host=ContainerHostName;")]
    [InlineData("OnlyHost", true, true, "Host=localhost;")] // host not replaced since no port
    [InlineData("OnlyPort", true, false, "Port=12345;")]
    [InlineData("OnlyPort", true, true, "Port=12345;")] // port not replaced since no host
    [InlineData("HostAndPort", true, false, "HostPort=ContainerHostName:12345")]
    [InlineData("HostAndPort", true, true, "HostPort=testresource:10000")] // host not replaced since no port
    [InlineData("PortBeforeHost", true, false, "Port=12345;Host=ContainerHostName;")]
    [InlineData("PortBeforeHost", true, true, "Port=10000;Host=testresource;")]
    [InlineData("FullAndPartial", true, false, "Test1=http://ContainerHostName:12345/;Test2=https://localhost:12346/;")]
    [InlineData("FullAndPartial", true, true, "Test1=http://testresource:10000/;Test2=https://localhost:12346/;")] // Second port not replaced since host is hard coded
    public async Task ExpressionResolverGeneratesCorrectEndpointStrings(string exprName, bool sourceIsContainer, bool targetIsContainer, string expectedConnectionString)
    {
        var builder = DistributedApplication.CreateBuilder();

        var target = builder.AddResource(new TestExpressionResolverResource(exprName))
            .WithEndpoint("endpoint1", e =>
            {
                e.UriScheme = "http";
                e.AllocatedEndpoint = new(e, "localhost", 12345, containerHostAddress: targetIsContainer ? "ContainerHostName" : null, targetPortExpression: "10000");
            })
            .WithEndpoint("endpoint2", e =>
             {
                 e.UriScheme = "https";
                 e.AllocatedEndpoint = new(e, "localhost", 12346, containerHostAddress: "ContainerHostName", targetPortExpression: "10001");
             });

        if (targetIsContainer)
        {
            target = target.WithImage("someimage");
        }

        // First test ExpressionResolver directly
        var csRef = new ConnectionStringReference(target.Resource, false);
        var connectionString = await ExpressionResolver.ResolveAsync(sourceIsContainer, csRef, "ContainerHostName", CancellationToken.None).DefaultTimeout();
        Assert.Equal(expectedConnectionString, connectionString.Value);

        // Then test it indirectly with a resource reference, which exercises a more complete code path
        var source = builder.AddResource(new ContainerResource("testSource"))
            .WithReference(target);
        if (sourceIsContainer)
        {
            source = source.WithImage("someimage");
        }

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(source.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance, "ContainerHostName").DefaultTimeout();
        Assert.Equal(expectedConnectionString, config["ConnectionStrings__testresource"]);
    }

    [Theory]
    [InlineData(false, "http://localhost:18889", "http://localhost:18889")]
    [InlineData(true, "http://localhost:18889", "http://ContainerHostName:18889")]
    [InlineData(false, "http://127.0.0.1:18889", "http://127.0.0.1:18889")]
    [InlineData(true, "http://127.0.0.1:18889", "http://ContainerHostName:18889")]
    [InlineData(false, "http://[::1]:18889", "http://[::1]:18889")]
    [InlineData(true, "http://[::1]:18889", "http://ContainerHostName:18889")]
    [InlineData(false, "Server=localhost,1433;User ID=sa;Password=xxx;Database=yyy", "Server=localhost,1433;User ID=sa;Password=xxx;Database=yyy")]
    [InlineData(true, "Server=localhost,1433;User ID=sa;Password=xxx;Database=yyy", "Server=ContainerHostName,1433;User ID=sa;Password=xxx;Database=yyy")]
    [InlineData(false, "Server=127.0.0.1,1433;User ID=sa;Password=xxx;Database=yyy", "Server=127.0.0.1,1433;User ID=sa;Password=xxx;Database=yyy")]
    [InlineData(true, "Server=127.0.0.1,1433;User ID=sa;Password=xxx;Database=yyy", "Server=ContainerHostName,1433;User ID=sa;Password=xxx;Database=yyy")]
    [InlineData(false, "Server=[::1],1433;User ID=sa;Password=xxx;Database=yyy", "Server=[::1],1433;User ID=sa;Password=xxx;Database=yyy")]
    [InlineData(true, "Server=[::1],1433;User ID=sa;Password=xxx;Database=yyy", "Server=ContainerHostName,1433;User ID=sa;Password=xxx;Database=yyy")]
    public async Task HostUrlPropertyGetsResolved(bool container, string hostUrlVal, string expectedValue)
    {
        var builder = DistributedApplication.CreateBuilder();

        var test = builder.AddResource(new ContainerResource("testSource"))
            .WithEnvironment(env =>
            {
                Assert.NotNull(env.Resource);

                env.EnvironmentVariables["envname"] = new HostUrl(hostUrlVal);
            });

        if (container)
        {
            test = test.WithImage("someimage");
        }

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(test.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance, "ContainerHostName").DefaultTimeout();
        Assert.Equal(expectedValue, config["envname"]);
    }

    [Theory]
    [InlineData(false, "http://localhost:18889")]
    [InlineData(true, "http://ContainerHostName:18889")]
    public async Task HostUrlPropertyGetsResolvedInOtlpExporterEndpoint(bool container, string expectedValue)
    {
        var builder = DistributedApplication.CreateBuilder();

        var test = builder.AddResource(new ContainerResource("testSource"))
            .WithOtlpExporter();

        if (container)
        {
            test = test.WithImage("someimage");
        }

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(test.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance, "ContainerHostName").DefaultTimeout();
        Assert.Equal(expectedValue, config["OTEL_EXPORTER_OTLP_ENDPOINT"]);
    }

    [Fact]
    public async Task ContainerToContainerEndpointShouldResolve()
    {
        var builder = DistributedApplication.CreateBuilder();

        var connectionStringResource = builder.AddResource(new MyContainerResource("myContainer"))
           .WithImage("redis")
           .WithHttpEndpoint(targetPort: 8080)
           .WithEndpoint("http", e =>
           {
               e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 8001, "ContainerHostName", "{{ targetPort }}");
           });

        var dep = builder.AddContainer("container", "redis")
           .WithReference(connectionStringResource)
           .WaitFor(connectionStringResource);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(dep.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance, "ContainerHostName").DefaultTimeout();

        Assert.Equal("http://myContainer:8080", config["ConnectionStrings__myContainer"]);
    }
}

sealed class MyContainerResource : ContainerResource, IResourceWithConnectionString
{
    public MyContainerResource(string name) : base(name)
    {
        PrimaryEndpoint = new(this, "http");
    }

    public EndpointReference PrimaryEndpoint { get; }

    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create(PrimaryEndpoint.Property(EndpointProperty.Url));
}

sealed class TestValueProviderResource(string name) : Resource(name), IValueProvider
{
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
    {
        return new ValueTask<string?>(base.Name);
    }
}

sealed class TestExpressionResolverResource : ContainerResource, IResourceWithEndpoints, IResourceWithConnectionString
{
    readonly string _exprName;
    EndpointReference Endpoint1 => new(this, "endpoint1");
    EndpointReference Endpoint2 => new(this, "endpoint2");
    Dictionary<string, ReferenceExpression> Expressions { get; }
    public TestExpressionResolverResource(string exprName) : base("testresource")
    {
        _exprName = exprName;

        Expressions = new()
        {
            { "TwoFullEndpoints", ReferenceExpression.Interpolate($"Test1={Endpoint1.Property(EndpointProperty.Scheme)}://{Endpoint1.Property(EndpointProperty.IPV4Host)}:{Endpoint1.Property(EndpointProperty.Port)}/;Test2={Endpoint2.Property(EndpointProperty.Scheme)}://{Endpoint2.Property(EndpointProperty.Host)}:{Endpoint2.Property(EndpointProperty.Port)}/;") },
            { "Url", ReferenceExpression.Interpolate($"Url={Endpoint1.Property(EndpointProperty.Url)};") },
            { "Url2", ReferenceExpression.Interpolate($"Url={Endpoint1};") },
            { "OnlyHost", ReferenceExpression.Interpolate($"Host={Endpoint1.Property(EndpointProperty.Host)};") },
            { "OnlyPort", ReferenceExpression.Interpolate($"Port={Endpoint1.Property(EndpointProperty.Port)};") },
            { "HostAndPort", ReferenceExpression.Interpolate($"HostPort={Endpoint1.Property(EndpointProperty.HostAndPort)}") },
            { "PortBeforeHost", ReferenceExpression.Interpolate($"Port={Endpoint1.Property(EndpointProperty.Port)};Host={Endpoint1.Property(EndpointProperty.Host)};") },
            { "FullAndPartial", ReferenceExpression.Interpolate($"Test1={Endpoint1.Property(EndpointProperty.Scheme)}://{Endpoint1.Property(EndpointProperty.IPV4Host)}:{Endpoint1.Property(EndpointProperty.Port)}/;Test2={Endpoint2.Property(EndpointProperty.Scheme)}://localhost:{Endpoint2.Property(EndpointProperty.Port)}/;") },
            { "Empty", ReferenceExpression.Create("") },
            { "String", ReferenceExpression.Create("String") },
            { "SecretParameter", ReferenceExpression.Create("SecretParameter", [new ParameterResource("SecretParameter", _ => "SecretParameter", secret: true)], []) },
            { "NonSecretParameter", ReferenceExpression.Create("NonSecretParameter", [new ParameterResource("NonSecretParameter", _ => "NonSecretParameter", secret: false)], []) }
        };
    }

    public ReferenceExpression ConnectionStringExpression => Expressions[_exprName];
}
