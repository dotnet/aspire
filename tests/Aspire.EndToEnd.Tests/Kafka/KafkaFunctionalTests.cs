// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.EndToEnd.Tests.Kafka;

[Collection("IntegrationServices")]
public class KafkaFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
{
    [Fact]
    public async Task KafkaComponentCanProduceAndConsume()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        string topic = $"topic-{Guid.NewGuid()}";

        var response = await integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/kafka/produce/{topic}", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, responseContent);

        response = await integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", $"/kafka/consume/{topic}", cts.Token);
        responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
