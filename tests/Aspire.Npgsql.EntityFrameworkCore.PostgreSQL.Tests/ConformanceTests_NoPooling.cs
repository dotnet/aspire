// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class ConformanceTests_NoPooling : ConformanceTests_Pooling
{
    protected override ServiceLifetime ServiceLifetime => ServiceLifetime.Scoped;

    protected override void RegisterComponent(HostApplicationBuilder builder, Action<NpgsqlEntityFrameworkCorePostgreSQLSettings>? configure = null, string? key = null)
    {
        // Configure Npgsql.EntityFrameworkCore.PostgreSQL services
        if (builder.Configuration.GetConnectionString("postgres") is string { } connectionString)
        {
            builder.Services.AddDbContextPool<TestDbContext>(dbContextOptionsBuilder => dbContextOptionsBuilder.UseNpgsql(connectionString));
        }

        builder.AddNpgsqlDbContext<TestDbContext>("postgres", configure);
    }
}
