// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.MongoDB;
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
    public async Task VerifyMongoWorks()
    {
        var testProgram = _integrationServicesFixture.TestProgram;
        var client = _integrationServicesFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response = await testProgram.IntegrationServiceABuilder!.HttpGetAsync(client, "http", "/mongodb/verify", cts.Token);
        var responseContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, responseContent);
    }

    [Fact]
    public void WithMongoExpressAddsContainer()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMongoDB("mongo").WithMongoExpress();

        Assert.Single(builder.Resources.OfType<MongoExpressContainerResource>());
    }

    [Fact]
    public void WithMongoExpressOnMultipleResources()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddMongoDB("mongo").WithMongoExpress();
        builder.AddMongoDB("mongo2").WithMongoExpress();

        Assert.Equal(2, builder.Resources.OfType<MongoExpressContainerResource>().Count());
    }
}
