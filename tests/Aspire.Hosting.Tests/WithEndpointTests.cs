// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithEndpointTests
{
    [Fact]
    public void EndpointsWithTwoPortsSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var testProgram = CreateTestProgram();
            testProgram.ServiceABuilder.WithEndpoint(3000, 1000, scheme: "https", name: "mybinding");
            testProgram.ServiceABuilder.WithEndpoint(3000, 2000, scheme: "https", name: "mybinding");
        });

        Assert.Equal("Endpoint with name 'mybinding' already exists", ex.Message);
    }

    [Fact]
    public void EndpointsWithSinglePortSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var testProgram = CreateTestProgram();
            testProgram.ServiceABuilder.WithEndpoint(1000, scheme: "https", name: "mybinding");
            testProgram.ServiceABuilder.WithEndpoint(2000, scheme: "https", name: "mybinding");
        });

        Assert.Equal("Endpoint with name 'mybinding' already exists", ex.Message);
    }

    [Fact]
    public void CanAddEndpointsWithContainerPortAndEnv()
    {
        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.AddExecutable("foo", "foo", ".")
                              .WithEndpoint(containerPort: 3001, scheme: "http", name: "mybinding", env: "PORT");

        var app = testProgram.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var exeResources = appModel.GetExecutableResources();

        var resource = Assert.Single(exeResources);
        Assert.Equal("foo", resource.Name);
        var endpoints = resource.Annotations.OfType<EndpointAnnotation>().ToArray();
        Assert.Single(endpoints);
        Assert.Equal("mybinding", endpoints[0].Name);
        Assert.Equal(3001, endpoints[0].ContainerPort);
        Assert.Equal("http", endpoints[0].UriScheme);
        Assert.Equal("PORT", endpoints[0].EnvironmentVariable);
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<WithEndpointTests>(args);

}
