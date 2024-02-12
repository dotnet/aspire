// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithEndpointTests
{
    [Fact]
    public void WithEndpointInvokesCallback()
    {
        var testProgram = CreateTestProgram();
        testProgram.ServiceABuilder.WithEndpoint(3000, 1000, name: "mybinding");
        testProgram.ServiceABuilder.WithEndpoint("mybinding", endpoint =>
        {
            endpoint.Port = 2000;
        });

        var endpoint = testProgram.ServiceABuilder.Resource.Annotations.OfType<EndpointAnnotation>().Single();
        Assert.Equal(2000, endpoint.Port);
    }

    [Fact]
    public void WithEndpointCallbackDoesNotRunIfEndpointDoesntExistAndCreateIfNotExistsIsFalse()
    {
        var executed = false;

        var testProgram = CreateTestProgram();
        testProgram.ServiceABuilder.WithEndpoint("mybinding", endpoint =>
        {
            executed = true;
        },
        createIfNotExists: false);

        Assert.False(executed);
    }

    [Fact]
    public void WithEndpointCallbackRunsIfEndpointDoesntExistAndCreateIfNotExistsIsDefault()
    {
        var executed = false;

        var testProgram = CreateTestProgram();
        testProgram.ServiceABuilder.WithEndpoint("mybinding", endpoint =>
        {
            executed = true;
        });

        Assert.True(executed);
    }

    [Fact]
    public void WithEndpointCallbackRunsIfEndpointDoesntExistAndCreateIfNotExistsIsTrue()
    {
        var executed = false;

        var testProgram = CreateTestProgram();
        testProgram.ServiceABuilder.WithEndpoint("mybinding", endpoint =>
        {
            executed = true;
        },
        createIfNotExists: true);

        Assert.True(executed);
    }

    [Fact]
    public void EndpointsWithTwoPortsSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var testProgram = CreateTestProgram();
            testProgram.ServiceABuilder.WithHttpsEndpoint(3000, 1000, name: "mybinding");
            testProgram.ServiceABuilder.WithHttpsEndpoint(3000, 2000, name: "mybinding");
        });

        Assert.Equal("Endpoint with name 'mybinding' already exists", ex.Message);
    }

    [Fact]
    public void EndpointsWithSinglePortSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var testProgram = CreateTestProgram();
            testProgram.ServiceABuilder.WithHttpsEndpoint(1000, name: "mybinding");
            testProgram.ServiceABuilder.WithHttpsEndpoint(2000, name: "mybinding");
        });

        Assert.Equal("Endpoint with name 'mybinding' already exists", ex.Message);
    }

    [Fact]
    public void CanAddEndpointsWithContainerPortAndEnv()
    {
        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.AddExecutable("foo", "foo", ".")
                              .WithHttpEndpoint(containerPort: 3001, name: "mybinding", env: "PORT");

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
