// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.EndToEnd.Tests.Redis;

[Collection("IntegrationServices")]
public class RedisFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public RedisFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [Fact]
    public async Task VerifyRedisWorks()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await _integrationServicesFixture.IntegrationServiceA.HttpGetAsync("http", "/redis/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }
}
