// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Pomelo.EntityFrameworkCore.MySql.Tests;

public class AspireEFMySqlExtensionsTests
{
    [Fact]
    public void CanConfigureDefaultSettings()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        bool? invoked = null, healthChecks = null, tracing = null, metrics = null;

        builder.Services.EnrichMySqlEntityFrameworkCore<TestDbContext>(builder, settings =>
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
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:HealthChecks", "false"),
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:Tracing", "false"),
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:Metrics", "false"),
        ]);

        bool? invoked = null, healthChecks = null, tracing = null, metrics = null;

        builder.Services.EnrichMySqlEntityFrameworkCore<TestDbContext>(builder, settings =>
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
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:TestDbContext:HealthChecks", "false"),
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:TestDbContext:Tracing", "false"),
            new KeyValuePair<string, string?>("Aspire:Pomelo:EntityFrameworkCore:MySql:TestDbContext:Metrics", "false"),
        ]);

        bool? invoked = null, healthChecks = null, tracing = null, metrics = null;

        builder.Services.EnrichMySqlEntityFrameworkCore<TestDbContext>(builder, settings =>
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
