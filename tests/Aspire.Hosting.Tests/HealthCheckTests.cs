// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Tests;

public class HealthCheckTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task WithHttpHealthCheckThrowsIfReferencingEndpointThatIsNotHttpScheme()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddContainer("resource", "dummycontainer")
                .WithEndpoint(targetPort: 9999, scheme: "tcp", name: "nonhttp")
                .WithHttpHealthCheck(endpointName: "nonhttp");

        using var app = builder.Build();

        var ex = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            await app.StartAsync();
        });

        Assert.Equal(
            "The endpoint 'nonhttp' on resource 'resource' was not using the 'http' scheme.",
            ex.Message
            );
    }

    [Fact]
    [RequiresDocker]
    public async Task WithHttpsHealthCheckThrowsIfReferencingEndpointThatIsNotHttpsScheme()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddContainer("resource", "dummycontainer")
                .WithEndpoint(targetPort: 9999, scheme: "tcp", name: "nonhttp")
                .WithHttpsHealthCheck(endpointName: "nonhttp");

        using var app = builder.Build();

        var ex = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            await app.StartAsync();
        });

        Assert.Equal(
            "The endpoint 'nonhttp' on resource 'resource' was not using the 'https' scheme.",
            ex.Message
            );
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyWithHttpHealthCheckBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddContainer("resource", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                              .WithHttpEndpoint(targetPort: 80)
                              .WithHttpHealthCheck(statusCode: 404)
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddContainer("dependentresource", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [Fact]
    public async Task BuildThrowsOnMissingHealthCheckRegistration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.Services.AddLogging(b => {
            b.AddXunit(testOutputHelper);
            b.AddFakeLogging();
        });

        builder.AddResource(new CustomResource("test"))
               .WithHealthCheck("test_check");
        var app = builder.Build();

        var ex = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
        {
            await app.StartAsync();
        });

        Assert.Equal("A health check registration is missing. Check logs for more details.", ex.Message);

        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        Assert.Contains(
            logs,
            l => l.Message == "The health check 'test_check' is not registered and is required for resource 'test'."
            );
    }

    private sealed class CustomChildResource(string name, CustomResource parent) : Resource(name), IResourceWithParent<CustomResource>
    {
        public CustomResource Parent => parent;
    }

    private sealed class CustomResource(string name) : Resource(name)
    {
    }
}
