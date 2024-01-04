// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
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

    [LocalOnlyFact]
    public async Task TestProjectStartsAndStopsCleanly()
    {
        var testProgram = _slimTestProgramFixture.TestProgram;
        var client = _slimTestProgramFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        // Make sure each service is running
        await testProgram.ServiceABuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceBBuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync(client, "http", cts.Token);
    }

    [LocalOnlyFact]
    public async Task TestPortOnEndpointAnnotationAndAllocatedEndpointAnnotationMatch()
    {
        var testProgram = _slimTestProgramFixture.TestProgram;
        var client = _slimTestProgramFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        // Make sure each service is running
        await testProgram.ServiceABuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceBBuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync(client, "http", cts.Token);

        foreach (var projectBuilders in testProgram.ServiceProjectBuilders)
        {
            var endpoint = projectBuilders.Resource.Annotations.OfType<EndpointAnnotation>().Single();
            var allocatedEndpoint = projectBuilders.Resource.Annotations.OfType<AllocatedEndpointAnnotation>().Single();

            Assert.Equal(endpoint.Port, allocatedEndpoint.Port);
        }
    }

    [LocalOnlyFact]
    public async Task TestPortOnEndpointAnnotationAndAllocatedEndpointAnnotationMatchForReplicatedServices()
    {
        var testProgram = _slimTestProgramFixture.TestProgram;
        var client = _slimTestProgramFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        // Make sure each service is running
        await testProgram.ServiceABuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceBBuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync(client, "http", cts.Token);

        foreach (var projectBuilders in testProgram.ServiceProjectBuilders)
        {
            var endpoint = projectBuilders.Resource.Annotations.OfType<EndpointAnnotation>().Single();
            var allocatedEndpoint = projectBuilders.Resource.Annotations.OfType<AllocatedEndpointAnnotation>().Single();

            Assert.Equal(endpoint.Port, allocatedEndpoint.Port);
        }
    }
}
