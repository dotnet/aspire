// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cosmos.Infrastructure.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos.Tests;

public class AspireAzureEfCoreCosmosDBExtensionsTests
{
    private const string ConnectionString = "AccountEndpoint=https://fake-account.documents.azure.com:443/;AccountKey=<fake-key>;";

    [Fact]
    public void CanConfigureDbContextOptions()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:cosmosConnection", ConnectionString),
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:Region", "westus"),
        ]);

        builder.AddCosmosDbContext<TestDbContext>("cosmosConnection", "databaseName", configureDbContextOptions: optionsBuilder =>
        {
            optionsBuilder.UseCosmos(ConnectionString, "databaseName", cosmosBuilder =>
            {
                cosmosBuilder.RequestTimeout(TimeSpan.FromSeconds(608));
            });
        });

        var host = builder.Build();
        var context = host.Services.GetRequiredService<TestDbContext>();

#pragma warning disable EF1001 // Internal EF Core API usage.

        var extension = context.Options.FindExtension<CosmosOptionsExtension>();
        Assert.NotNull(extension);

        // Ensure the RequestTimeout from config size was respected
        Assert.Equal(TimeSpan.FromSeconds(608), extension.RequestTimeout);

        // Ensure the Region from the lambda was respected
        Assert.Equal("westus", extension.Region);

#pragma warning restore EF1001 // Internal EF Core API usage.
    }
}
