// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithEndpointTests
{
    [Fact]
    public async Task WithEndpointInvokesCallback()
    {
        using var testProgram = CreateTestProgram();
        testProgram.ServiceABuilder.WithEndpoint(3000, 1000, name: "mybinding");
        testProgram.ServiceABuilder.WithEndpoint("mybinding", endpoint =>
        {
            endpoint.Port = 2000;
        });

        // Throw before ApplicationExecutor starts doing real work
        testProgram.AppBuilder.Services.AddLifecycleHook<ThrowLifecycleHook>();

        var app = testProgram.Build();

        // Don't want to actually start an app
        await Assert.ThrowsAnyAsync<Exception>(() => app.StartAsync());

        var endpoints = testProgram.ServiceABuilder.Resource.Annotations.OfType<EndpointAnnotation>();
        Assert.Collection(endpoints,
            e1 => Assert.Equal(2000, e1.Port),
            e2 => Assert.Equal("http", e2.Name));
    }

    [Fact]
    public async Task WithEndpointCallbackRunsIfEndpointDoesntExistAndCreateIfNotExistsIsTrue()
    {
        var executed = false;

        using var testProgram = CreateTestProgram();
        testProgram.ServiceABuilder.WithEndpoint("mybinding", endpoint =>
        {
            executed = true;
        });

        Assert.False(executed);
        Assert.False(testProgram.ServiceABuilder.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out _));

        // Throw before ApplicationExecutor starts doing real work
        testProgram.AppBuilder.Services.AddLifecycleHook<ThrowLifecycleHook>();

        var app = testProgram.Build();

        // Don't want to actually start an app
        await Assert.ThrowsAnyAsync<Exception>(() => app.StartAsync());

        Assert.True(executed);
        Assert.True(testProgram.ServiceABuilder.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out _));
    }

    [Fact]
    public void EndpointsWithTwoPortsSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            using var testProgram = CreateTestProgram();
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
            using var testProgram = CreateTestProgram();
            testProgram.ServiceABuilder.WithHttpsEndpoint(1000, name: "mybinding");
            testProgram.ServiceABuilder.WithHttpsEndpoint(2000, name: "mybinding");
        });

        Assert.Equal("Endpoint with name 'mybinding' already exists", ex.Message);
    }

    [Fact]
    public void CanAddEndpointsWithContainerPortAndEnv()
    {
        using var testProgram = CreateTestProgram();
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

    [Fact]
    public async Task DeferredEndpointMutationWorks()
    {
        using var testProgram = CreateTestProgram();
        var executed = false;
        testProgram.ServiceABuilder.WithEndpoint("http", endpoint =>
        {
            executed = true;
            endpoint.IsProxied = false;
            endpoint.IsExternal = true;
            endpoint.Port = 1;
        });

        Assert.False(executed);
        Assert.False(testProgram.ServiceABuilder.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out _));

        // Throw before ApplicationExecutor starts doing real work
        testProgram.AppBuilder.Services.AddLifecycleHook<ThrowLifecycleHook>();

        var app = testProgram.Build();

        // Don't want to actually start an app
        await Assert.ThrowsAnyAsync<Exception>(() => app.StartAsync());

        Assert.True(executed);
        Assert.True(testProgram.ServiceABuilder.Resource.TryGetAnnotationsOfType<EndpointAnnotation>(out var endpointAnnotations));
        var endpoint = Assert.Single(endpointAnnotations);
        Assert.True(endpoint.IsExternal);
        Assert.False(endpoint.IsProxied);
        Assert.Equal(1, endpoint.Port);
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<WithEndpointTests>(args);

    private sealed class ThrowLifecycleHook : IDistributedApplicationLifecycleHook
    {
        public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException();
        }
    }

}
