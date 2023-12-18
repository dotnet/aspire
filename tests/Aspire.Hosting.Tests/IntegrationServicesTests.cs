// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Xunit;

namespace Aspire.Hosting.Tests;

[Collection("IntegrationServices")]
public class IntegrationServicesTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public IntegrationServicesTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [LocalOnlyFact]
    public async Task VerifyHealthyOnIntegrationServiceA()
    {
        var testProgram = _integrationServicesFixture.TestProgram;
        var client = _integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        // Make sure all services are running
        await testProgram.ServiceABuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceBBuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.ServiceCBuilder.HttpGetPidAsync(client, "http", cts.Token);
        await testProgram.IntegrationServiceABuilder!.HttpGetPidAsync(client, "http", cts.Token);

        // We wait until timeout for the /health endpoint to return successfully. We assume
        // that components wired up into this project have health checks enabled.
        await testProgram.IntegrationServiceABuilder!.WaitForHealthyStatusAsync(client, "http", cts.Token);
    }
}
