// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Xunit;

namespace Aspire.Hosting.Tests.RabbitMQ;

[Collection("IntegrationServices")]
public class RabbitMQFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public RabbitMQFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [LocalOnlyFact()]
    public async Task VerifyRabbitMQWorks()
    {
        var testProgram = _integrationServicesFixture.TestProgram;
        var client = _integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", "/rabbit/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
