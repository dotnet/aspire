// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Xunit;

namespace Aspire.Hosting.Tests.Kafka;

[Collection("IntegrationServices")]
public class KafkaFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public KafkaFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [LocalOnlyFact]
    public async Task KafkaComponentCanProduceAndConsume()
    {
        var testProgram = _integrationServicesFixture.TestProgram;

        var client = _integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        string topic = $"topic-{Guid.NewGuid()}";

        HttpResponseMessage response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", $"/kafka/produce/{topic}", cts.Token);
        Assert.Equal("100", await response.Content.ReadAsStringAsync());

        response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", $"/kafka/consume/{topic}", cts.Token);
        Assert.Equal("100", await response.Content.ReadAsStringAsync());
    }
}
