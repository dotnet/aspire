// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.EndToEnd.Tests;

public class IntegrationServicesTests : IClassFixture<IntegrationServicesFixture>
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public IntegrationServicesTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [LocalOnlyTheory]
    [InlineData("cosmos")]
    [InlineData("mongodb")]
    [InlineData("mysql")]
    [InlineData("pomelo")]
    [InlineData("oracledatabase")]
    [InlineData("postgres")]
    [InlineData("rabbitmq")]
    [InlineData("redis")]
    [InlineData("sqlserver")]
    public async Task VerifyComponentWorks(string component)
    {
        var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/{component}/verify");
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }

    [LocalOnlyFact]
    public async Task KafkaComponentCanProduceAndConsume()
    {
        string topic = $"topic-{Guid.NewGuid()}";

        var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/kafka/produce/{topic}");
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, responseContent);

        response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/kafka/consume/{topic}");
        responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, responseContent);
    }

    [LocalOnlyFact]
    public async Task VerifyHealthyOnIntegrationServiceA()
    {
        // We wait until timeout for the /health endpoint to return successfully. We assume
        // that components wired up into this project have health checks enabled.
        await _integrationServicesFixture.IntegrationServiceA.WaitForHealthyStatusAsync("http");
    }
}

// TODO: remove these attributes when the above tests are running in CI

public class LocalOnlyFactAttribute : FactAttribute
{
    public override string Skip
    {
        get
        {
            // BUILD_BUILDID is defined by Azure Dev Ops

            if (Environment.GetEnvironmentVariable("BUILD_BUILDID") != null)
            {
                return "LocalOnlyFactAttribute tests are not run as part of CI.";
            }

            return null!;
        }
    }
}

public class LocalOnlyTheoryAttribute : TheoryAttribute
{
    public override string Skip
    {
        get
        {
            // BUILD_BUILDID is defined by Azure Dev Ops

            if (Environment.GetEnvironmentVariable("BUILD_BUILDID") != null)
            {
                return "LocalOnlyTheoryAttribute tests are not run as part of CI.";
            }

            return null!;
        }
    }
}
