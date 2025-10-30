// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Microsoft.AspNetCore.InternalTesting;

namespace Aspire.Hosting.Tests;

public class ExpressionResolverTests
{
    [Theory]
    [MemberData(nameof(ResolveInternalAsync_ResolvesCorrectly_MemberData))]
    public async Task ResolveInternalAsync_ResolvesCorrectly(ExpressionResolverTestData testData, Type? exceptionType, (string Value, bool IsSensitive)? expectedValue)
    {
        ValueProviderContext context = new ValueProviderContext()
        {
            Network = testData.SourceIsContainer ? KnownNetworkIdentifiers.DefaultAspireContainerNetwork : KnownNetworkIdentifiers.LocalhostNetwork
        };
        if (exceptionType is not null)
        {
            await Assert.ThrowsAsync(exceptionType, ResolveAsync);
        }
        else
        {
            var resolvedValue = await ExpressionResolver.ResolveAsync(testData.ValueProvider, context, CancellationToken.None);

            Assert.Equal(expectedValue?.Value, resolvedValue.Value);
            Assert.Equal(expectedValue?.IsSensitive, resolvedValue.IsSensitive);
        }

        async Task<ResolvedValue> ResolveAsync() => await ExpressionResolver.ResolveAsync(testData.ValueProvider, context, CancellationToken.None);
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
    [InlineData("TwoFullEndpoints", true, false, "Test1=http://aspire.dev.internal:22345/;Test2=https://aspire.dev.internal:22346/;")]
    [InlineData("TwoFullEndpoints", true, true, "Test1=http://testresource:22345/;Test2=https://testresource:22346/;")]
    [InlineData("Url", false, false, "Url=http://localhost:12345;")]
    [InlineData("Url", false, true, "Url=http://localhost:12345;")]
    [InlineData("Url", true, false, "Url=http://aspire.dev.internal:22345;")]
    [InlineData("Url", true, true, "Url=http://testresource:22345;")]
    [InlineData("Url2", true, false, "Url=http://aspire.dev.internal:22345;")]
    [InlineData("Url2", true, true, "Url=http://testresource:22345;")]
    [InlineData("OnlyHost", true, false, "Host=aspire.dev.internal;")]
    [InlineData("OnlyHost", true, true, "Host=testresource;")]
    [InlineData("OnlyPort", true, false, "Port=22345;")]
    [InlineData("OnlyPort", true, true, "Port=22345;")]
    [InlineData("HostAndPort", true, false, "HostPort=aspire.dev.internal:22345")]
    [InlineData("HostAndPort", true, true, "HostPort=testresource:22345")]
    [InlineData("PortBeforeHost", true, false, "Port=22345;Host=aspire.dev.internal;")]
    [InlineData("PortBeforeHost", true, true, "Port=22345;Host=testresource;")]
    [InlineData("FullAndPartial", true, false, "Test1=http://aspire.dev.internal:22345/;Test2=https://localhost:22346/;")]
    [InlineData("FullAndPartial", true, true, "Test1=http://testresource:22345/;Test2=https://localhost:22346/;")]
    [InlineData("UrlEncodedHost", false, false, "Host=host%20with%20space;")]
    public async Task ExpressionResolverGeneratesCorrectEndpointStrings(string exprName, bool sourceIsContainer, bool targetIsContainer, string expectedConnectionString)
    {
        var builder = DistributedApplication.CreateBuilder();

        var target = builder.AddResource(new TestExpressionResolverResource(exprName))
            .WithEndpoint("endpoint1", e =>
            {
                e.UriScheme = "http";
                e.AllocatedEndpoint = new(e, "localhost", 12345, targetPortExpression: "10000");
                if (sourceIsContainer)
                {
                    // Note: on the container network side the port and target port are always the same for AllocatedEndpoint.
                    var ae = new AllocatedEndpoint(e, KnownHostNames.DefaultContainerTunnelHostName, 22345, EndpointBindingMode.SingleAddress, targetPortExpression: "22345", KnownNetworkIdentifiers.DefaultAspireContainerNetwork);
                    var snapshot = new ValueSnapshot<AllocatedEndpoint>();
                    snapshot.SetValue(ae);
                    e.AllAllocatedEndpoints.TryAdd(KnownNetworkIdentifiers.DefaultAspireContainerNetwork, snapshot);
                }
            })
            .WithEndpoint("endpoint2", e =>
             {
                 e.UriScheme = "https";
                 e.AllocatedEndpoint = new(e, "localhost", 12346, targetPortExpression: "10001");
                 if (sourceIsContainer)
                 {
                     var ae = new AllocatedEndpoint(e, KnownHostNames.DefaultContainerTunnelHostName, 22346, EndpointBindingMode.SingleAddress, targetPortExpression: "22346", KnownNetworkIdentifiers.DefaultAspireContainerNetwork);
                     var snapshot = new ValueSnapshot<AllocatedEndpoint>();
                     snapshot.SetValue(ae);
                     e.AllAllocatedEndpoints.TryAdd(KnownNetworkIdentifiers.DefaultAspireContainerNetwork, snapshot);
                 }
             })
             .WithEndpoint("endpoint3", e =>
             {
                 e.UriScheme = "https";
                 e.AllocatedEndpoint = new(e, "host with space", 12347);
                 if (sourceIsContainer)
                 {
                     var ae = new AllocatedEndpoint(e, KnownHostNames.DefaultContainerTunnelHostName, 22347, EndpointBindingMode.SingleAddress, targetPortExpression: "22346", KnownNetworkIdentifiers.DefaultAspireContainerNetwork);
                     var snapshot = new ValueSnapshot<AllocatedEndpoint>();
                     snapshot.SetValue(ae);
                     e.AllAllocatedEndpoints.TryAdd(KnownNetworkIdentifiers.DefaultAspireContainerNetwork, snapshot);
                 }
             });

        if (targetIsContainer)
        {
            target = target.WithImage("someimage");
        }

        // First test ExpressionResolver directly
        var csRef = new ConnectionStringReference(target.Resource, false);
        var context = new ValueProviderContext()
        {
            Network = sourceIsContainer ? KnownNetworkIdentifiers.DefaultAspireContainerNetwork : KnownNetworkIdentifiers.LocalhostNetwork
        };
        var connectionString = await ExpressionResolver.ResolveAsync(csRef, context, CancellationToken.None).DefaultTimeout();
        Assert.Equal(expectedConnectionString, connectionString.Value);

        // Then test it indirectly with a resource reference, which exercises a more complete code path
        var source = builder.AddResource(new ContainerResource("testSource"))
            .WithReference(target);
        if (sourceIsContainer)
        {
            source = source.WithImage("someimage");
        }

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(source.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();
        Assert.Equal(expectedConnectionString, config["ConnectionStrings__testresource"]);
    }

    [Theory]
    [InlineData(false, "http://localhost:18889", "http://localhost:18889")]
    [InlineData(true, "http://localhost:18889", "http://aspire.dev.internal:18889")]
    [InlineData(false, "http://127.0.0.1:18889", "http://127.0.0.1:18889")]
    [InlineData(true, "http://127.0.0.1:18889", "http://aspire.dev.internal:18889")]
    [InlineData(false, "http://[::1]:18889", "http://[::1]:18889")]
    [InlineData(true, "http://[::1]:18889", "http://aspire.dev.internal:18889")]
    [InlineData(false, "Server=localhost,1433;User ID=sa;Password=xxx;Database=yyy", "Server=localhost,1433;User ID=sa;Password=xxx;Database=yyy")]
    [InlineData(true, "Server=localhost,1433;User ID=sa;Password=xxx;Database=yyy", "Server=aspire.dev.internal,1433;User ID=sa;Password=xxx;Database=yyy")]
    [InlineData(false, "Server=127.0.0.1,1433;User ID=sa;Password=xxx;Database=yyy", "Server=127.0.0.1,1433;User ID=sa;Password=xxx;Database=yyy")]
    [InlineData(true, "Server=127.0.0.1,1433;User ID=sa;Password=xxx;Database=yyy", "Server=aspire.dev.internal,1433;User ID=sa;Password=xxx;Database=yyy")]
    [InlineData(false, "Server=[::1],1433;User ID=sa;Password=xxx;Database=yyy", "Server=[::1],1433;User ID=sa;Password=xxx;Database=yyy")]
    [InlineData(true, "Server=[::1],1433;User ID=sa;Password=xxx;Database=yyy", "Server=aspire.dev.internal,1433;User ID=sa;Password=xxx;Database=yyy")]
    public async Task HostUrlPropertyGetsResolved(bool targetIsContainer, string hostUrlVal, string expectedValue)
    {
        var builder = DistributedApplication.CreateBuilder();

        var test = builder.AddResource(new ContainerResource("testSource"))
            .WithEnvironment(env =>
            {
                Assert.NotNull(env.Resource);

                env.EnvironmentVariables["envname"] = new HostUrl(hostUrlVal);
            });

        if (targetIsContainer)
        {
            test = test.WithImage("someimage");
        }

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(test.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();
        Assert.Equal(expectedValue, config["envname"]);
    }

    [Theory]
    [InlineData(false, "http://localhost:18889")]
    [InlineData(true, "http://aspire.dev.internal:18889")]
    public async Task HostUrlPropertyGetsResolvedInOtlpExporterEndpoint(bool container, string expectedValue)
    {
        var builder = DistributedApplication.CreateBuilder();

        var test = builder.AddResource(new ContainerResource("testSource"))
            .WithOtlpExporter();

        if (container)
        {
            test = test.WithImage("someimage");
        }

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(test.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();
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
               e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 8001, EndpointBindingMode.SingleAddress, "{{ targetPort }}", KnownNetworkIdentifiers.LocalhostNetwork);
           });

        var dep = builder.AddContainer("container", "redis")
           .WithReference(connectionStringResource)
           .WaitFor(connectionStringResource);

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(dep.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        Assert.Equal("http://myContainer:8080", config["ConnectionStrings__myContainer"]);
    }
}

sealed class MyContainerResource : ContainerResource, IResourceWithConnectionString
{
    public MyContainerResource(string name) : base(name)
    {
        PrimaryEndpoint = new(this, "http", KnownNetworkIdentifiers.LocalhostNetwork);
    }

    public EndpointReference PrimaryEndpoint { get; }

    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create($"{PrimaryEndpoint.Property(EndpointProperty.Url)}");
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
    EndpointReference Endpoint1 => new(this, "endpoint1", KnownNetworkIdentifiers.LocalhostNetwork);
    EndpointReference Endpoint2 => new(this, "endpoint2", KnownNetworkIdentifiers.LocalhostNetwork);
    EndpointReference Endpoint3 => new(this, "endpoint3", KnownNetworkIdentifiers.LocalhostNetwork);
    Dictionary<string, ReferenceExpression> Expressions { get; }
    public TestExpressionResolverResource(string exprName) : base("testresource")
    {
        _exprName = exprName;

        Expressions = new()
        {
            { "TwoFullEndpoints", ReferenceExpression.Create($"Test1={Endpoint1.Property(EndpointProperty.Scheme)}://{Endpoint1.Property(EndpointProperty.IPV4Host)}:{Endpoint1.Property(EndpointProperty.Port)}/;Test2={Endpoint2.Property(EndpointProperty.Scheme)}://{Endpoint2.Property(EndpointProperty.Host)}:{Endpoint2.Property(EndpointProperty.Port)}/;") },
            { "Url", ReferenceExpression.Create($"Url={Endpoint1.Property(EndpointProperty.Url)};") },
            { "Url2", ReferenceExpression.Create($"Url={Endpoint1};") },
            { "OnlyHost", ReferenceExpression.Create($"Host={Endpoint1.Property(EndpointProperty.Host)};") },
            { "OnlyPort", ReferenceExpression.Create($"Port={Endpoint1.Property(EndpointProperty.Port)};") },
            { "HostAndPort", ReferenceExpression.Create($"HostPort={Endpoint1.Property(EndpointProperty.HostAndPort)}") },
            { "PortBeforeHost", ReferenceExpression.Create($"Port={Endpoint1.Property(EndpointProperty.Port)};Host={Endpoint1.Property(EndpointProperty.Host)};") },
            { "FullAndPartial", ReferenceExpression.Create($"Test1={Endpoint1.Property(EndpointProperty.Scheme)}://{Endpoint1.Property(EndpointProperty.IPV4Host)}:{Endpoint1.Property(EndpointProperty.Port)}/;Test2={Endpoint2.Property(EndpointProperty.Scheme)}://localhost:{Endpoint2.Property(EndpointProperty.Port)}/;") },
            { "Empty", ReferenceExpression.Empty },
            { "String", ReferenceExpression.Create($"String") },
            { "SecretParameter", ReferenceExpression.Create("SecretParameter", [new ParameterResource("SecretParameter", _ => "SecretParameter", secret: true)], [], [null]) },
            { "NonSecretParameter", ReferenceExpression.Create("NonSecretParameter", [new ParameterResource("NonSecretParameter", _ => "NonSecretParameter", secret: false)], [], [null]) },
            { "UrlEncodedHost", ReferenceExpression.Create($"Host={Endpoint3.Property(EndpointProperty.Host):uri};") },
        };
    }

    public ReferenceExpression ConnectionStringExpression => Expressions[_exprName];
}
