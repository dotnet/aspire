// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.EndToEnd.Tests.SqlServer;

[Collection("IntegrationServices")]
public class SqlServerFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public SqlServerFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [Fact]
    public async Task VerifySqlServerWorks()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", "/sqlserver/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
