// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Xunit;

namespace Aspire.Hosting.Tests.Postgres;

[Collection("IntegrationServices")]
public class PostgresFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public PostgresFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [LocalOnlyFact()]
    public async Task VerifyPostgresWorks()
    {
        var testProgram = _integrationServicesFixture.TestProgram;
        var client = _integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", "/postgres/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
