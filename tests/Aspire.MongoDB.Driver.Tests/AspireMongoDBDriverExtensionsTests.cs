// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Xunit;

namespace Aspire.MongoDB.Driver.Tests;

public class AspireMongoDBDriverExtensionsTests
{
    private const string DefaultConnectionString = "mongodb://localhost:27017/mydatabase";
    private const string DefaultConnectionName = "mongodb";

    [Theory]
    [InlineData("mongodb://localhost:27017/mydatabase", true)]
    [InlineData("mongodb://localhost:27017", false)]
    public void AddMongoDBDataSource_ReadsFromConnectionStringsCorrectly(string connectionString, bool shouldRegisterDatabase)
    {
        var builder = CreateBuilder(connectionString);

        builder.AddMongoDBClient(DefaultConnectionName);

        var host = builder.Build();

        var mongoClient = host.Services.GetRequiredService<IMongoClient>();

        var uri = MongoUrl.Create(connectionString);

        Assert.Equal(uri.Server.Host, mongoClient.Settings.Server.Host);
        Assert.Equal(uri.Server.Port, mongoClient.Settings.Server.Port);

        var mongoDatabase = host.Services.GetService<IMongoDatabase>();

        if (shouldRegisterDatabase)
        {
            Assert.NotNull(mongoDatabase);
            Assert.Equal(uri.DatabaseName, mongoDatabase.DatabaseNamespace.DatabaseName);
        }
        else
        {
            Assert.Null(mongoDatabase);
        }
    }

    [Theory]
    [InlineData("mongodb://localhost:27017/mydatabase", true)]
    [InlineData("mongodb://localhost:27017", false)]
    public void AddKeyedMongoDBDataSource_ReadsFromConnectionStringsCorrectly(string connectionString, bool shouldRegisterDatabase)
    {
        var key = DefaultConnectionName;

        var builder = CreateBuilder(connectionString);

        builder.AddKeyedMongoDBClient(key);

        var host = builder.Build();

        var mongoClient = host.Services.GetRequiredKeyedService<IMongoClient>(key);

        var uri = MongoUrl.Create(connectionString);

        Assert.Equal(uri.Server.Host, mongoClient.Settings.Server.Host);
        Assert.Equal(uri.Server.Port, mongoClient.Settings.Server.Port);

        var mongoDatabase = host.Services.GetKeyedService<IMongoDatabase>(key);

        if (shouldRegisterDatabase)
        {
            Assert.NotNull(mongoDatabase);
            Assert.Equal(uri.DatabaseName, mongoDatabase.DatabaseNamespace.DatabaseName);
        }
        else
        {
            Assert.Null(mongoDatabase);
        }
    }

    [Fact]
    public async Task AddMongoDBDataSource_HealthCheckShouldBeRegisteredWhenEnabled()
    {
        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddMongoDBClient(DefaultConnectionName, settings =>
        {
            settings.HealthChecks = true;
            settings.HealthCheckTimeout = 1;
        });

        var host = builder.Build();

        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();

        var healthCheckReport = await healthCheckService.CheckHealthAsync();

        var healthCheckName = "MongoDB.Driver";

        Assert.Contains(healthCheckReport.Entries, x => x.Key == healthCheckName);
    }

    [Fact]
    public void AddKeyedMongoDBDataSource_HealthCheckShouldNotBeRegisteredWhenDisabled()
    {
        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddKeyedMongoDBClient(DefaultConnectionName, settings =>
        {
            settings.HealthChecks = false;
        });

        var host = builder.Build();

        var healthCheckService = host.Services.GetService<HealthCheckService>();

        Assert.Null(healthCheckService);

    }

    [Fact]
    public async Task AddKeyedMongoDBDataSource_HealthCheckShouldBeRegisteredWhenEnabled()
    {
        var key = DefaultConnectionName;

        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddKeyedMongoDBClient(key, settings =>
        {
            settings.HealthChecks = true;
            settings.HealthCheckTimeout = 1;
        });

        var host = builder.Build();

        var healthCheckService = host.Services.GetRequiredService<HealthCheckService>();

        var healthCheckReport = await healthCheckService.CheckHealthAsync();

        var healthCheckName = $"MongoDB.Driver_{key}";

        Assert.Contains(healthCheckReport.Entries, x => x.Key == healthCheckName);
    }

    [Fact]
    public void AddMongoDBDataSource_HealthCheckShouldNotBeRegisteredWhenDisabled()
    {
        var builder = CreateBuilder(DefaultConnectionString);

        builder.AddMongoDBClient(DefaultConnectionName, settings =>
        {
            settings.HealthChecks = false;
        });

        var host = builder.Build();

        var healthCheckService = host.Services.GetService<HealthCheckService>();

        Assert.Null(healthCheckService);
    }

    private static HostApplicationBuilder CreateBuilder(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>($"ConnectionStrings:{DefaultConnectionName}", connectionString)
        ]);
        return builder;
    }
}
