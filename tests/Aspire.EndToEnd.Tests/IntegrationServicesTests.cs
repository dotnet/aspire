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
    public async Task VerifyComponentWorks(TestResourceNames resourceName)
    {
        _integrationServicesFixture.EnsureAppHostRunning();

        try
        {
            var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/{resourceName}/verify");
            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, responseContent);
        }
        catch
        {
            _testOutput.WriteLine ($"[{DateTime.Now}] <<<< FAILED VerifyComponentWorks for {resourceName} --");
            await _integrationServicesFixture.DumpComponentLogsAsync(resourceName.ToString().ToLowerInvariant(), _testOutput);
            await _integrationServicesFixture.DumpDockerInfoAsync();

            throw;
        }
    }

    // FIXME: open issue
    [ConditionalTheory]
    [SkipOnCI("not working on CI yet")]
    [InlineData(TestResourceNames.cosmos)]
    [InlineData(TestResourceNames.oracledatabase)]
    public Task VerifyComponentWorksDisabledOnCI(TestResourceNames resourceName)
    {
        if (resourceName == TestResourceNames.cosmos && RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            throw new SkipException($"Skipping '{resourceName}' test because the emulator isn't supported on macOS ARM64.");
        }

        return VerifyComponentWorks(resourceName);
    }

    [Fact]
    public async Task KafkaComponentCanProduceAndConsume()
    {
        try
        {
            _integrationServicesFixture.EnsureAppHostRunning();

            string topic = $"topic-{Guid.NewGuid()}";

            var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/kafka/produce/{topic}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, responseContent);

            response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/kafka/consume/{topic}");
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, responseContent);
        }
        catch
        {
            _testOutput.WriteLine($"[{DateTime.Now}] <<<< FAILED KafkaComponentCanProduceAndConsume --");
            await _integrationServicesFixture.DumpDockerInfoAsync();
            throw;
        }
    }

    [Fact]
    public async Task VerifyHealthyOnIntegrationServiceA()
    {
        try
        {
            _integrationServicesFixture.EnsureAppHostRunning();

            // We wait until timeout for the /health endpoint to return successfully. We assume
            // that components wired up into this project have health checks enabled.
            await _integrationServicesFixture.IntegrationServiceA.WaitForHealthyStatusAsync("http");
        }
        catch
        {
            _testOutput.WriteLine($"[{DateTime.Now}] <<<< FAILED VerifyHealthyOnIntegrationServiceA --");
            await _integrationServicesFixture.DumpDockerInfoAsync();
            throw;
        }
    }
}
