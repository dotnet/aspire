// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class EnrichNpgsqlTests : ConformanceTests
{
    protected override void RegisterComponent(HostApplicationBuilder builder, Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configure = null, string? key = null)
    {
        builder.Services.AddDbContextPool<TestDbContext>(options => options.UseNpgsql(ConnectionString));
        builder.EnrichNpgsqlDbContext<TestDbContext>(configure);
    }

    [Fact]
    public void ShouldThrowIfDbContextIsNotRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        Assert.Throws<InvalidOperationException>(() => builder.EnrichNpgsqlDbContext<TestDbContext>());
    }

    [Fact]
    public void ShouldNotThrowIfDbContextIsRegistered()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);

        builder.Services.AddDbContext<TestDbContext>(options => options.UseNpgsql(ConnectionString));

        builder.EnrichNpgsqlDbContext<TestDbContext>();
    }
}
