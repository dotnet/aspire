// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Aspire.TestProject;
using Aspire.Workload.Tests;

namespace Aspire.EndToEnd.Tests;

public class IntegrationServicesTests : IClassFixture<IntegrationServicesFixture>
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;
    private readonly TestOutputWrapper _testOutput;

    public IntegrationServicesTests(ITestOutputHelper testOutput, IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
        _testOutput = new TestOutputWrapper(testOutput);
    }

    [Theory]
    [InlineData(TestResourceNames.mongodb)]
    [InlineData(TestResourceNames.mysql)]
    [InlineData(TestResourceNames.pomelo)]
    [InlineData(TestResourceNames.postgres)]
    [InlineData(TestResourceNames.rabbitmq)]
    [InlineData(TestResourceNames.redis)]
    [InlineData(TestResourceNames.sqlserver)]
    [InlineData(TestResourceNames.efnpgsql)]
    public Task VerifyComponentWorks(TestResourceNames resourceName)
        => RunTestAsync(async () =>
        {
            try
            {
                var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/{resourceName}/verify");
                var responseContent = await response.Content.ReadAsStringAsync();

                Assert.True(response.IsSuccessStatusCode, responseContent);
            }
            catch
            {
                await _integrationServicesFixture.DumpComponentLogsAsync(resourceName.ToString().ToLowerInvariant(), _testOutput);
                throw;
            }
        });

    // FIXME: open issue
    [ConditionalTheory]
    [SkipOnCI("not working on CI yet")]
    [InlineData(TestResourceNames.cosmos)]
    [InlineData(TestResourceNames.oracledatabase)]
    public Task VerifyComponentWorksDisabledOnCI(TestResourceNames resourceName)
    {
        if (resourceName == TestResourceNames.cosmos && RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            throw SkipException.ForSkip($"Skipping '{resourceName}' test because the emulator isn't supported on macOS ARM64.");
        }

        return VerifyComponentWorks(resourceName);
    }

    [Fact]
    public Task KafkaComponentCanProduceAndConsume()
        => RunTestAsync(async() =>
        {
            string topic = $"topic-{Guid.NewGuid()}";

            var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/kafka/produce/{topic}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, responseContent);

            response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/kafka/consume/{topic}");
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, responseContent);
        });

    [Fact]
    public Task VerifyHealthyOnIntegrationServiceA()
        => RunTestAsync(async () =>
        {
            // We wait until timeout for the /health endpoint to return successfully. We assume
            // that components wired up into this project have health checks enabled.
            await _integrationServicesFixture.IntegrationServiceA.WaitForHealthyStatusAsync("http", _testOutput);
        });

    private async Task RunTestAsync(Func<Task> test)
    {
        _integrationServicesFixture.EnsureAppHostRunning();
        try
        {
            await test();
        }
        catch
        {
            await _integrationServicesFixture.DumpDockerInfoAsync();
            throw;
        }
    }
}
