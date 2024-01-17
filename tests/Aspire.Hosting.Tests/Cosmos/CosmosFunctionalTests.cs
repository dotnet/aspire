// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Polly;
using Polly.Retry;
using Xunit;

namespace Aspire.Hosting.Tests.Cosmos;

[Collection("IntegrationServices")]
public class CosmosFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public CosmosFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [LocalOnlyFact()]
    public async Task VerifyCosmosWorks()
    {
        var testProgram = _integrationServicesFixture.TestProgram;
        var client = _integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6));

        await RetryPolicy.Handle<HttpRequestException>()
                         .WaitAndRetryAsync(20, (count) => TimeSpan.FromSeconds(15))
                         .ExecuteAsync(async () =>
                         {
                             var response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", "/cosmos/verify", cts.Token);
                             response.EnsureSuccessStatusCode();

                             var responseContent = await response.Content.ReadAsStringAsync();
                             Assert.True(response.IsSuccessStatusCode, responseContent);
                         });
    }
}
