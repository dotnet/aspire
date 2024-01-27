// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos.Tests;

public class AspireAzureEfCoreCosmosDBExtensionsTests
{

    [Fact]
    public void CanConfigureDefaultSettings()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        bool? invoked = null, tracing = null, metrics = null;

        builder.Services.EnrichCosmosDbEntityFrameworkCore<TestDbContext>(builder, settings =>
        {
            invoked = true;
            tracing = settings.Tracing;
            metrics = settings.Metrics;
        });

        var host = builder.Build();

        Assert.True(invoked);
        Assert.True(tracing);
        Assert.True(metrics);
    }

    [Fact]
    public void CanBindDefaultSection()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:HealthChecks", "false"),
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:Tracing", "false"),
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:Metrics", "false"),
        ]);

        bool? invoked = null, tracing = null, metrics = null;

        builder.Services.EnrichCosmosDbEntityFrameworkCore<TestDbContext>(builder, settings =>
        {
            invoked = true;
            tracing = settings.Tracing;
            metrics = settings.Metrics;
        });

        var host = builder.Build();

        Assert.True(invoked);
        Assert.False(tracing);
        Assert.False(metrics);
    }

    [Fact]
    public void CanBindTypeSpecificSection()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:TestDbContext:HealthChecks", "false"),
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:TestDbContext:Tracing", "false"),
            new KeyValuePair<string, string?>("Aspire:Microsoft:EntityFrameworkCore:Cosmos:TestDbContext:Metrics", "false"),
        ]);

        bool? invoked = null, tracing = null, metrics = null;

        builder.Services.EnrichCosmosDbEntityFrameworkCore<TestDbContext>(builder, settings =>
        {
            invoked = true;
            tracing = settings.Tracing;
            metrics = settings.Metrics;
        });

        var host = builder.Build();

        Assert.True(invoked);
        Assert.False(tracing);
        Assert.False(metrics);
    }
}
