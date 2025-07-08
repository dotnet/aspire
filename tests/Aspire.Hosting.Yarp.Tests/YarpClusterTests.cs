// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Yarp.Tests;

public class YarpClusterTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Create_YarpCluster_From_Endpoints_With_Names()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var resource = builder.AddResource(new TestResource("ServiceA"))
                              .WithHttpEndpoint(name: "testendpoint")
                              .WithHttpsEndpoint(name: "anotherendpoint");

        var httpEndpoint = resource.GetEndpoint("testendpoint");
        var httpsEndpoint = resource.GetEndpoint("anotherendpoint");

        var httpCluster = new YarpCluster(httpEndpoint);
        Assert.Equal("http://_testendpoint.ServiceA", httpCluster.Target);

        var httpsCluster = new YarpCluster(httpsEndpoint);
        Assert.Equal("https://_anotherendpoint.ServiceA", httpsCluster.Target);
    }

    [Fact]
    public void Create_YarpCluster_From_Endpoints_Without_Names()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var resource = builder.AddResource(new TestResource("ServiceC"))
                              .WithHttpEndpoint()
                              .WithHttpsEndpoint();

        var httpEndpoint = resource.GetEndpoint("http");
        var httpsEndpoint = resource.GetEndpoint("https");

        var httpCluster = new YarpCluster(httpEndpoint);
        Assert.Equal("http://_http.ServiceC", httpCluster.Target);

        var httpsCluster = new YarpCluster(httpsEndpoint);
        Assert.Equal("https://_https.ServiceC", httpsCluster.Target);
    }

    [Fact]
    public void Create_YarpCluster_From_Resource_With_One_Endpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var httpService = builder.AddResource(new TestResource("ServiceC"))
                                 .WithHttpEndpoint()
                                 .WithHttpEndpoint(name: "grpc");

        var httpCluster = new YarpCluster(httpService.Resource);
        Assert.Equal($"http://ServiceC", httpCluster.Target);

        var httpsService = builder.AddResource(new TestResource("ServiceD"))
                                  .WithHttpsEndpoint()
                                  .WithHttpsEndpoint(name: "grpc");

        var httpsCluster = new YarpCluster(httpsService.Resource);
        Assert.Equal($"https://ServiceD", httpsCluster.Target);
    }

    [Fact]
    public void Create_YarpCluster_From_Resource_With_Both_Endpoints()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var serviceA = builder.AddResource(new TestResource("ServiceA"))
                              .WithHttpEndpoint()
                              .WithHttpsEndpoint();

        var clusterA = new YarpCluster(serviceA.Resource);
        Assert.Equal($"https+http://ServiceA", clusterA.Target);
    }

    private sealed class TestResource(string name) : IResourceWithServiceDiscovery
    {
        public string Name => name;

        public ResourceAnnotationCollection Annotations { get; } = new ResourceAnnotationCollection();
    }
}
