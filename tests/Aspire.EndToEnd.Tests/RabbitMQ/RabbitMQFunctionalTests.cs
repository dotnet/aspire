// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.EndToEnd.Tests.RabbitMQ;

[Collection("IntegrationServices")]
public class RabbitMQFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public RabbitMQFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [Fact]
    public async Task VerifyRabbitMQWorks()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", "/rabbitmq/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
