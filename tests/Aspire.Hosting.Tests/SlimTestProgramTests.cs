// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Testing;
// using Aspire.Hosting.Tests.Helpers;
using Xunit;

namespace Aspire.Hosting.Tests;

[Collection("SlimTestProgram")]
public class SlimTestProgramTests
{
    private readonly SlimTestProgramFixture _slimTestProgramFixture;

    public SlimTestProgramTests(SlimTestProgramFixture slimTestProgramFixture)
    {
        _slimTestProgramFixture = slimTestProgramFixture;
    }

    [Fact]
    public async Task TestProjectStartsAndStopsCleanly()
    {
        var testProgram = _slimTestProgramFixture.TestProgram;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        // Make sure each service is running
        await EnsureServicesAreRunning(testProgram, cts.Token);
    }

    private static async Task EnsureServicesAreRunning(TestProgram testProgram, CancellationToken cancellationToken)
    {
        var app = testProgram.App!;
        using var clientA = app.CreateHttpClient(testProgram.ServiceABuilder.Resource.Name, "http");
        await clientA.GetStringAsync("/", cancellationToken);

        using var clientB = app.CreateHttpClient(testProgram.ServiceBBuilder.Resource.Name, "http");
        await clientB.GetStringAsync("/", cancellationToken);

        using var clientC = app.CreateHttpClient(testProgram.ServiceCBuilder.Resource.Name, "http");
        await clientC.GetStringAsync("/", cancellationToken);
    }

    [Fact]
    public async Task TestPortOnEndpointAnnotationAndAllocatedEndpointAnnotationMatch()
    {
        var testProgram = _slimTestProgramFixture.TestProgram;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        // Make sure each service is running
        await EnsureServicesAreRunning(testProgram, cts.Token);

        foreach (var projectBuilders in testProgram.ServiceProjectBuilders)
        {
            var endpoint = projectBuilders.Resource.Annotations.OfType<EndpointAnnotation>().Single();
            Assert.NotNull(endpoint.AllocatedEndpoint);
            Assert.Equal(endpoint.Port, endpoint.AllocatedEndpoint.Port);
        }
    }

    [Fact]
    public async Task TestPortOnEndpointAnnotationAndAllocatedEndpointAnnotationMatchForReplicatedServices()
    {
        var testProgram = _slimTestProgramFixture.TestProgram;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        // Make sure each service is running
        await EnsureServicesAreRunning(testProgram, cts.Token);

        foreach (var projectBuilders in testProgram.ServiceProjectBuilders)
        {
            var endpoint = projectBuilders.Resource.Annotations.OfType<EndpointAnnotation>().Single();
            Assert.NotNull(endpoint.AllocatedEndpoint);
            Assert.Equal(endpoint.Port, endpoint.AllocatedEndpoint.Port);
        }
    }
}
