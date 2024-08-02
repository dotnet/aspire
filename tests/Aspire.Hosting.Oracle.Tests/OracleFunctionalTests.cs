// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Oracle.Tests;

public class OracleFunctionalTests(ITestOutputHelper testOutputHelper)
{
    private const string DatabaseReadyText = "Completed: ALTER DATABASE OPEN";

    [Fact]
    [RequiresDocker]
    public async Task VerifyEfOracle()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                MaxRetryAttempts = int.MaxValue,
                BackoffType = DelayBackoffType.Linear,
                Delay = TimeSpan.FromSeconds(2)
            })
            .Build();

        var builder = CreateDistributedApplicationBuilder();

        var oracleDbName = "freepdb1";

        var oracle = builder.AddOracle("oracle");

        var db = oracle.AddDatabase(oracleDbName);

        using var app = builder.Build();

        await app.StartAsync(cts.Token);
        await app.WaitForText(DatabaseReadyText).WaitAsync(TimeSpan.FromMinutes(2));

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration[$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default);

        hb.AddOracleDatabaseDbContext<TestDbContext>(db.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        var dbContext = host.Services.GetRequiredService<TestDbContext>();
        var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();

        Assert.True(await databaseCreator.CanConnectAsync(cts.Token));

        await databaseCreator.CreateTablesAsync(cts.Token);

        dbContext.Cars.Add(new TestDbContext.Car { Brand = "BatMobile" });
        await dbContext.SaveChangesAsync(cts.Token);

        var cars = await dbContext.Cars.ToListAsync(cts.Token);
        Assert.Single(cars);
        Assert.Equal("BatMobile", cars[0].Brand);
    }

    private TestDistributedApplicationBuilder CreateDistributedApplicationBuilder()
    {
        // Do not use the test container registry as Oracle has their own
        var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(config => config.SetMinimumLevel(LogLevel.Information));
        builder.Services.AddXunitLogging(testOutputHelper);
        builder.Services.AddHostedService<ResourceLoggerForwarderService>();
        return builder;
    }
}
