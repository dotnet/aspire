// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using Aspire.Hosting.Tests.Helpers;
using Xunit;

namespace Aspire.Hosting.Tests.MongoDB;

[Collection("IntegrationServices")]
public class MongoDBFunctionalTests
{
    private readonly IntegrationServicesFixture _integrationServicesFixture;

    public MongoDBFunctionalTests(IntegrationServicesFixture integrationServicesFixture)
    {
        _integrationServicesFixture = integrationServicesFixture;
    }

    [LocalOnlyFact()]
    public async Task DatabaseIsCreatedOnDemand()
    {
        var testProgram = _integrationServicesFixture.TestProgram;
        var client = _integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", "/mongodb/databases", cts.Token);
        var databases = await response.Content.ReadFromJsonAsync<string[]>(cts.Token);

        Assert.Equivalent(new[] { "admin", "config", "local", "mymongodb" }, databases);
    }

    [LocalOnlyFact()]
    public async Task VerifyMongoWorks()
    {
        var testProgram = _integrationServicesFixture.TestProgram;
        var client = _integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", "/mongodb/movies", cts.Token);
        var movies = await response.Content.ReadFromJsonAsync<string[]>(cts.Token);

        Assert.Equivalent(new[] { "Rocky I", "Rocky II" }, movies);
    }
}
