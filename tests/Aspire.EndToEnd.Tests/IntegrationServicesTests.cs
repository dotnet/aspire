// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

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
    [InlineData("mongodb")]
    [InlineData("mysql")]
    [InlineData("pomelo")]
    [InlineData("postgres")]
    [InlineData("rabbitmq")]
    [InlineData("redis")]
    [InlineData("sqlserver")]
    public async Task VerifyComponentWorks(string component)
    {
        _integrationServicesFixture.EnsureAppHostRunning();

        _testOutput.WriteLine ($"[{DateTime.Now}] >>>> Starting VerifyComponentWorks for {component} --");
        try
        {
            var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/{component}/verify");
            var responseContent = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, responseContent);
            _testOutput.WriteLine ($"[{DateTime.Now}] <<<< Done VerifyComponentWorks for {component} --");
        }
        catch
        {
            _testOutput.WriteLine ($"[{DateTime.Now}] <<<< FAILED VerifyComponentWorks for {component} --");
            await _integrationServicesFixture.DumpComponentLogsAsync(component, _testOutput);
            await _integrationServicesFixture.DumpDockerInfoAsync();

            throw;
        }
    }

    // FIXME: open issue
    [ConditionalTheory]
    [SkipOnCI("not working on CI yet")]
    [InlineData("cosmos")]
    [InlineData("oracledatabase")]
    public Task VerifyComponentWorksDisabledOnCI(string component)
    {
        if (component == "cosmos" && RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            throw new SkipException("Skipping 'cosmos' test because the emulator isn't supported on macOS ARM64.");
        }

        return VerifyComponentWorks(component);
    }

    [Fact]
    public async Task KafkaComponentCanProduceAndConsume()
    {
        _testOutput.WriteLine($"[{DateTime.Now}] >>>> Starting KafkaComponentCanProduceAndConsume --");
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
            _testOutput.WriteLine ($"[{DateTime.Now}] <<<< Done KafkaComponentCanProduceAndConsume --");
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
        _testOutput.WriteLine($"[{DateTime.Now}] >>>> Starting VerifyHealthyOnIntegrationServiceA --");

        try
        {
            _integrationServicesFixture.EnsureAppHostRunning();

            // We wait until timeout for the /health endpoint to return successfully. We assume
            // that components wired up into this project have health checks enabled.
            await _integrationServicesFixture.IntegrationServiceA.WaitForHealthyStatusAsync("http");
            _testOutput.WriteLine($"[{DateTime.Now}] <<<< Done VerifyHealthyOnIntegrationServiceA --");
        }
        catch
        {
            _testOutput.WriteLine($"[{DateTime.Now}] <<<< FAILED VerifyHealthyOnIntegrationServiceA --");
            await _integrationServicesFixture.DumpDockerInfoAsync();
            throw;
        }
    }
}

// FIXME: remove?
public static class TestComponents
{
    public static string Cosmos => "cosmos";
    public static string Mongodb => "mongodb";
    public static string Mysql => "mysql";
    public static string Pomelo => "pomelo";
    public static string Oracledatabase => "oracledatabase";
    public static string Postgres => "postgres";
    public static string Rabbitmq => "rabbitmq";
    public static string Redis => "redis";
    public static string Sqlserver => "sqlserver";
    public static string Kafka => "kafka";

}
