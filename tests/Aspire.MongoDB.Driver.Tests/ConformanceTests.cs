// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Xunit;

namespace Aspire.MongoDB.Driver.Tests;

public class ConformanceTests : ConformanceTests<IMongoClient, MongoDBSettings>
{
    private const string ConnectionSting = "mongodb://root:password@localhost:27017/test_db";

    private static readonly Lazy<bool> s_canConnectToServer = new(GetCanConnect);

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    protected override string ActivitySourceName => "MongoDB.Driver.Core.Extensions.DiagnosticSources";

    protected override string JsonSchemaPath => "src/Components/Aspire.MongoDB.Driver/ConfigurationSchema.json";

    protected override bool SupportsKeyedRegistrations => true;

    protected override bool CanConnectToServer => s_canConnectToServer.Value;

    protected override string ValidJsonConfig => """
        {
          "Aspire": {
            "MongoDB": {
              "Driver": {
                "ConnectionString": "YOUR_CONNECTION_STRING",
                "HealthChecks": true,
                "HealthCheckTimeout": 100,
                "Tracing": true
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
    {
        ("""{"Aspire": { "MongoDB":{ "Driver": { "HealthChecks": "true"}}}}""", "Value is \"string\" but should be \"boolean\""),
        ("""{"Aspire": { "MongoDB":{ "Driver": { "HealthCheckTimeout": "10000"}}}}""", "Value is \"string\" but should be \"integer\""),
        ("""{"Aspire": { "MongoDB":{ "Driver": { "Tracing": "true"}}}}""", "Value is \"string\" but should be \"boolean\""),
    };

    protected override string[] RequiredLogCategories => [
        "MongoDB.SDAM",
        "MongoDB.ServerSelection",
        "MongoDB.Connection",
    ];

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>(
                CreateConfigKey("Aspire:MongoDB:Driver", key, "ConnectionString"),
                ConnectionSting)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<MongoDBSettings>? configure = null, string? key = null)
    {
        if (key is null)
        {
            builder.AddMongoDBClient("mongodb", configure);
        }
        else
        {
            builder.AddKeyedMongoDBClient(key, configure);
        }
    }

    protected override void SetHealthCheck(MongoDBSettings options, bool enabled)
    {
        options.HealthChecks = enabled;
        options.HealthCheckTimeout = 10;
    }

    protected override void SetMetrics(MongoDBSettings options, bool enabled) => throw new NotImplementedException();

    protected override void SetTracing(MongoDBSettings options, bool enabled)
    {
        options.Tracing = enabled;
    }

    protected override void TriggerActivity(IMongoClient service)
    {
        using var source = new CancellationTokenSource(10);

        service.ListDatabases(source.Token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("key")]
    public void ClientAndDatabaseInstancesShouldBeResolved(string? key)
    {
        using IHost host = CreateHostWithComponent(key: key);

        IMongoClient? mongoClient = Resolve<IMongoClient>();
        IMongoDatabase? mongoDatabase = Resolve<IMongoDatabase>();

        Assert.NotNull(mongoClient);
        Assert.NotNull(mongoDatabase);

        T? Resolve<T>() => key is null ? host.Services.GetService<T>() : host.Services.GetKeyedService<T>(key);
    }

    private static bool GetCanConnect()
    {
        var client = new MongoClient(ConnectionSting);

        try
        {
            client.ListDatabaseNames();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
