// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Xunit;

namespace Aspire.Hosting.Tests.Kafka;

[Collection("IntegrationServices")]
public class KafkaFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
{
    [LocalOnlyFact]
    public async Task KafkaComponentCanProduceAndConsume()
    {
        var testProgram = integrationServicesFixture.TestProgram;

        var client = integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        string topic = $"topic-{Guid.NewGuid()}";

        var response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", $"/kafka/produce/{topic}", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, responseContent);

        response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", $"/kafka/consume/{topic}", cts.Token);
        responseContent = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
