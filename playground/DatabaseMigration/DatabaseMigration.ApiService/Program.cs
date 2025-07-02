// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<MyDb1Context>("db1", configureDbContextOptions: options =>
{
    options.UseSqlServer(sqlServerOptions =>
    {
        sqlServerOptions.MigrationsAssembly("DatabaseMigration.ApiModel");
    });
});

var app = builder.Build();

app.MapGet("/", async (MyDb1Context context) =>
{
    var entry = new Entry();
    await context.Entries.AddAsync(entry);
    await context.SaveChangesAsync();

    var entries = await context.Entries.ToListAsync();

    return new
    {
        totalEntries = entries.Count,
        entries = entries
    };
});

app.Run();
