// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using Aspire.TestProject;
using Aspire.Workload.Tests;
using Microsoft.DotNet.XUnitExtensions;

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
    [Trait("scenario", "basicservices")]
    [InlineData(TestResourceNames.postgres)]
    [InlineData(TestResourceNames.efnpgsql)]
    [InlineData(TestResourceNames.redis)]
    public Task VerifyComponentWorks(TestResourceNames resourceName)
        => RunTestAsync(async () =>
        {
            _integrationServicesFixture.EnsureAppHasResources(resourceName);
            try
            {
                var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/{resourceName}/verify");
                var responseContent = await response.Content.ReadAsStringAsync();

                Assert.True(response.IsSuccessStatusCode, responseContent);
            }
            catch
            {
                await _integrationServicesFixture.DumpComponentLogsAsync(resourceName, _testOutput);
                throw;
            }
        });

    [ActiveIssue("https://github.com/dotnet/aspire/issues/5820")]
    [ConditionalTheory]
    [Trait("scenario", "cosmos")]
    [InlineData(TestResourceNames.cosmos)]
    [InlineData(TestResourceNames.efcosmos)]
    public Task VerifyCosmosComponentWorks(TestResourceNames resourceName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            throw new SkipTestException($"Skipping 'cosmos' test because the emulator isn't supported on macOS ARM64.");
        }

        return VerifyComponentWorks(resourceName);
    }

    [Fact]
    // Include all the scenarios here so this test gets run for all of them.
    // https://github.com/dotnet/aspire/issues/5820
    // [Trait("scenario", "cosmos")]
    [Trait("scenario", "basicservices")]
    public Task VerifyHealthyOnIntegrationServiceA()
        => RunTestAsync(async () =>
        {
            // We wait until timeout for the /health endpoint to return successfully. We assume
            // that components wired up into this project have health checks enabled.
            await _integrationServicesFixture.IntegrationServiceA.WaitForHealthyStatusAsync("http", _testOutput);
        });

    private async Task RunTestAsync(Func<Task> test)
    {
        _integrationServicesFixture.Project.EnsureAppHostRunning();
        try
        {
            await test();
        }
        catch
        {
            await _integrationServicesFixture.Project.DumpDockerInfoAsync();
            throw;
        }
    }
}
