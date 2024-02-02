// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.EndToEnd.Tests.Cosmos;

[Collection("IntegrationServices")]
public class CosmosFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public CosmosFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [Fact]
    public async Task VerifyCosmosWorks()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6));

        var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", "/cosmos/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
