// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.ConformanceTests;
using Aspire.MongoDB.Driver.Tests;
using Aspire.TestUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.MongoDB.EntityFrameworkCore.Tests;

public class ConformanceTests : ConformanceTests<TestDbContext, MongoDBEntityFrameworkCoreSettings>, IClassFixture<MongoDbContainerFixture>
{
    // in the future it can become a static property that reads the value from Env Var
    private readonly MongoDbContainerFixture? _containerFixture;
    protected string ConnectionString { get; private set; }
    protected override ServiceLifetime ServiceLifetime { get; }
    protected override string ActivitySourceName => "MongoDB.Driver.Core.Extensions.DiagnosticSources";
    protected override string[] RequiredLogCategories => new string[]
    {
        "Microsoft.EntityFrameworkCore.Infrastructure",
        "Microsoft.EntityFrameworkCore.ChangeTracking",
        "Microsoft.EntityFrameworkCore.Infrastructure",
        "Microsoft.EntityFrameworkCore.Database.Command",
        "Microsoft.EntityFrameworkCore.Query",
        "Microsoft.EntityFrameworkCore.Database.Transaction",
        "Microsoft.EntityFrameworkCore.Database.Connection",
        "Microsoft.EntityFrameworkCore.Model",
        "Microsoft.EntityFrameworkCore.Model.Validation",
        "Microsoft.EntityFrameworkCore.Update",
        "Microsoft.EntityFrameworkCore.Migrations"
    };

    protected override string ValidJsonConfig => """
         {
           "Aspire": {
             "MongoDB": {
               "EntityFrameworkCore": {
                 "ConnectionString": "YOUR_CONNECTION_STRING",
                 "DisableHealthChecks": false,
                 "HealthCheckTimeout": 100,
                 "DisableTracing": false
               }
             }
           }
         }
         """;
    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
    {
        ("""{"Aspire": { "MongoDB":{ "EntityFrameworkCore": { "DisableHealthChecks": "true"}}}}""", "Value is \"string\" but should be \"boolean\""),
        ("""{"Aspire": { "MongoDB":{ "EntityFrameworkCore": { "HealthCheckTimeout": "10000"}}}}""", "Value is \"string\" but should be \"integer\""),
        ("""{"Aspire": { "MongoDB":{ "EntityFrameworkCore": { "DisableTracing": "true"}}}}""", "Value is \"string\" but should be \"boolean\""),
    };

    public ConformanceTests(MongoDbContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
        ConnectionString = (_containerFixture is not null && RequiresDockerAttribute.IsSupported)
            ? _containerFixture.GetConnectionString()
            : "mongodb://localhost:27017/test_aspire_mongodb";
    }

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<MongoDBEntityFrameworkCoreSettings>? configure = null, string? key = null)
        => builder.AddMongoDBDatabaseDbContext<TestDbContext>(key ?? "mongodb", configure);

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new("Aspire:MongoDB:EntityFrameworkCore:ConnectionString", ConnectionString)
        });

    protected override void TriggerActivity(TestDbContext service)
    {
        if (service.Database.CanConnect())
        {
            service.Database.EnsureCreated();
        }
    }

    protected override void SetHealthCheck(MongoDBEntityFrameworkCoreSettings options, bool enabled)
        => options.DisableHealthChecks = !enabled;

    protected override void SetTracing(MongoDBEntityFrameworkCoreSettings options, bool enabled)
        => options.DisableTracing = !enabled;

    protected override void SetMetrics(MongoDBEntityFrameworkCoreSettings options, bool enabled) => throw new NotImplementedException();
}
