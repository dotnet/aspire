// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Xunit;

namespace Aspire.Hosting.Tests.MySql;

[Collection("IntegrationServices")]
public class MySqlFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public MySqlFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [LocalOnlyFact()]
    public async Task VerifyMySqlWorks()
    {
        // MySql health check reports healthy during temporary server phase, c.f. https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/issues/2031
        // This is mitigated by standard resilience handlers in the IntegrationServicesFixture HttpClient configuration

        var testProgram = _integrationServicesFixture.TestProgram;
        var client = _integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", "/mysql/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
