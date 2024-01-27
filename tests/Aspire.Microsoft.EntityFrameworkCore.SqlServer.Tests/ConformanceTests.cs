// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Components.ConformanceTests;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.EntityFrameworkCore.SqlServer.Tests;

public class ConformanceTests : ConformanceTests<TestDbContext, MicrosoftEntityFrameworkCoreSqlServerSettings>
{
    protected const string ConnectionString = "Host=fake;Database=catalog";

    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Singleton;

    // https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/cb5b2193ef9cacc0b9ef699e085022577551bf85/src/OpenTelemetry.Instrumentation.EntityFrameworkCore/Implementation/EntityFrameworkDiagnosticListener.cs#L38
    protected override string ActivitySourceName => "OpenTelemetry.Instrumentation.EntityFrameworkCore";

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
            "Microsoft": {
              "EntityFrameworkCore": {
                "SqlServer": {
                  "HealthChecks": false,
                  "Tracing": true,
                  "Metrics": true
                }
              }
            }
          }
        }
        """;

    protected override (string json, string error)[] InvalidJsonToErrorMessage => new[]
        {

            ("""{"Aspire": { "Microsoft": { "EntityFrameworkCore":{ "SqlServer": { "HealthChecks": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Microsoft": { "EntityFrameworkCore":{ "SqlServer": { "Tracing": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
            ("""{"Aspire": { "Microsoft": { "EntityFrameworkCore":{ "SqlServer": { "Metrics": "false"}}}}}""", "Value is \"string\" but should be \"boolean\""),
        };

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:SqlServer:ConnectionString", ConnectionString)
        });

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<MicrosoftEntityFrameworkCoreSqlServerSettings>? configure = null, string? key = null)
    {
        var connectionString = builder.Configuration.GetValue<string>("Aspire:Microsoft:EntityFrameworkCore:SqlServer:ConnectionString");
        builder.Services.AddDbContextPool<TestDbContext>(dbContextOptionsBuilder => dbContextOptionsBuilder.UseSqlServer(connectionString))
            .EnrichSqlServerEntityFrameworkCore<TestDbContext>(builder, configure);
    }

    protected override void SetHealthCheck(MicrosoftEntityFrameworkCoreSqlServerSettings options, bool enabled)
        => options.HealthChecks = enabled;

    protected override void SetTracing(MicrosoftEntityFrameworkCoreSqlServerSettings options, bool enabled)
        => options.Tracing = enabled;

    protected override void SetMetrics(MicrosoftEntityFrameworkCoreSqlServerSettings options, bool enabled)
        => options.Metrics = enabled;

    protected override void TriggerActivity(TestDbContext service)
    {
        if (service.Database.CanConnect())
        {
            service.Database.EnsureCreated();
        }
    }

    protected override void SetupConnectionInformationIsDelayValidated()
    {
        throw new SkipTestException("EF doesn't require a connection string");
    }

    [ConditionalFact]
    public void TracingEnablesTheRightActivitySource()
    {
        SkipIfCanNotConnectToServer();

        RemoteExecutor.Invoke(() => ActivitySourceTest(key: null)).Dispose();
    }
}
