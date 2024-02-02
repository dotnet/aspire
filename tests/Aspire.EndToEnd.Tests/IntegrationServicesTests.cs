// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.EndToEnd.Tests;

[Collection("IntegrationServices")]
public class IntegrationServicesTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public IntegrationServicesTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [Fact]
    public async Task VerifyHealthyOnIntegrationServiceA()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        // We wait until timeout for the /health endpoint to return successfully. We assume
        // that components wired up into this project have health checks enabled.
        await _integrationServicesFixture.IntegrationServiceA.WaitForHealthyStatusAsync("http", cts.Token);
    }
}
