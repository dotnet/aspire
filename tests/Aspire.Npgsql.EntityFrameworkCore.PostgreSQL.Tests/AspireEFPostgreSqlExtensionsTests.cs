// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class AspireEFPostgreSqlExtensionsTests
{
    [Fact]
    public void CanConfigureDefaultSettings()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        bool? invoked = null, healthChecks = null, tracing = null, metrics = null;

        builder.Services.EnrichNpgsqlEntityFrameworkCore<TestDbContext>(builder, settings =>
        {
            invoked = true;
            healthChecks = settings.HealthChecks;
            tracing = settings.Tracing;
            metrics = settings.Metrics;
        });

        var host = builder.Build();

        Assert.True(invoked);
        Assert.True(healthChecks);
        Assert.True(tracing);
        Assert.True(metrics);
    }

    [Fact]
    public void CanBindDefaultSection()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:HealthChecks", "false"),
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:Tracing", "false"),
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:Metrics", "false"),
        ]);

        bool? invoked = null, healthChecks = null, tracing = null, metrics = null;

        builder.Services.EnrichNpgsqlEntityFrameworkCore<TestDbContext>(builder, settings =>
        {
            invoked = true;
            healthChecks = settings.HealthChecks;
            tracing = settings.Tracing;
            metrics = settings.Metrics;
        });

        var host = builder.Build();

        Assert.True(invoked);
        Assert.False(healthChecks);
        Assert.False(tracing);
        Assert.False(metrics);
    }

    [Fact]
    public void CanBindTypeSpecificSection()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:TestDbContext:HealthChecks", "false"),
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:TestDbContext:Tracing", "false"),
            new KeyValuePair<string, string?>("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:TestDbContext:Metrics", "false"),
        ]);

        bool? invoked = null, healthChecks = null, tracing = null, metrics = null;

        builder.Services.EnrichNpgsqlEntityFrameworkCore<TestDbContext>(builder, settings =>
        {
            invoked = true;
            healthChecks = settings.HealthChecks;
            tracing = settings.Tracing;
            metrics = settings.Metrics;
        });

        var host = builder.Build();

        Assert.True(invoked);
        Assert.False(healthChecks);
        Assert.False(tracing);
        Assert.False(metrics);
    }
}
