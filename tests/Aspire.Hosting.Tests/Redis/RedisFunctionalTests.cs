// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Xunit;

namespace Aspire.Hosting.Tests.Redis;

[Collection("IntegrationServices")]
public class RedisFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public RedisFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [LocalOnlyFact()]
    public async Task VerifyRedisWorks()
    {
        var testProgram = _integrationServicesFixture.TestProgram;
        var client = _integrationServicesFixture.HttpClient;
        var data = "Hello World!";

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        await testProgram.IntegrationServiceABuilder!.HttpPostAsync(client, "http", "/redis/hello", new StringContent(data), cts.Token);
        var response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", "/redis/hello", cts.Token);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(data, content);
    }
}
