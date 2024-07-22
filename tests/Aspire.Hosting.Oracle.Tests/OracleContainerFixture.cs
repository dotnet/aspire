// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;

namespace Aspire.Hosting.Oracle.Tests;
public sealed class OracleContainerFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var pipeline = new ResiliencePipelineBuilder()
                .AddRetry(new() { MaxRetryAttempts = int.MaxValue, Delay = TimeSpan.FromSeconds(10) })
                .Build();

            var builder = TestDistributedApplicationBuilder.Create();
            builder.Services.AddLogging(config => config.SetMinimumLevel(LogLevel.Information));
            builder.Services.AddHostedService<ResourceLoggerForwarderService>();

            var oracle = builder.AddOracle("oracle");
            var db = oracle.AddDatabase("freepdb1");

            using var app = builder.Build();

            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default)
            });

            hb.AddOracleDatabaseDbContext<TestDbContext>(db.Resource.Name);

            using var host = hb.Build();

            await host.StartAsync();

            // Wait until the database is available
            await pipeline.ExecuteAsync(async token =>
            {
                var dbContext = host.Services.GetRequiredService<TestDbContext>();
                var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();
                Assert.True(await databaseCreator.CanConnectAsync(token));
            }, cts.Token);

            // Stops the container
            await app.StopAsync();
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

}
