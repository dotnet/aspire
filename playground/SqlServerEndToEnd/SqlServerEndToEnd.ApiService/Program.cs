// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using SqlServerEndToEnd.Common;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<MyDb1Context>("db1");
builder.AddSqlServerDbContext<MyDb2Context>("db2");

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", async (MyDb1Context db1Context, MyDb2Context db2Context) =>
{
    var entry1 = new Entry();
    await db1Context.Entries.AddAsync(entry1);
    await db1Context.SaveChangesAsync();

    var entry2 = new Entry();
    await db2Context.Entries.AddAsync(entry2);
    await db2Context.SaveChangesAsync();

    var entries1 = await db1Context.Entries.ToListAsync();
    var entries2 = await db2Context.Entries.ToListAsync();

    return new
    {
        totalEntries = entries1.Count + entries2.Count,
        entries1 = entries1,
        entries2 = entries2
    };
});

app.Run();
