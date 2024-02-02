// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.EndToEnd.Tests.MySql;

[Collection("IntegrationServices")]
public class MySqlFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public MySqlFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [Fact]
    public async Task VerifyMySqlWorks()
    {
        // MySql health check reports healthy during temporary server phase, c.f. https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/issues/2031
        // This is mitigated by standard resilience handlers in the IntegrationServicesFixture HttpClient configuration

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", "/mysql/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
