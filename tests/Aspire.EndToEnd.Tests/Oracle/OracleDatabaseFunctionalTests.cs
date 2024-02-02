// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.EndToEnd.Tests.Oracle;

[Collection("IntegrationServices")]
public class OracleDatabaseFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public OracleDatabaseFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [Fact]
    public async Task VerifyOracleDatabaseWorks()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", "/oracledatabase/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
