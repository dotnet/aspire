// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Publishers.Common;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<MyDbContext>("db");

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", async (MyDbContext dbContext) =>
{
    // We only work with db1Context for the rest of this
    // since we've proven connectivity to the others for now.
    var entry = new Entry();
    await dbContext.Entries.AddAsync(entry);
    await dbContext.SaveChangesAsync();

    var entries = await dbContext.Entries.ToListAsync();

    return new
    {
        totalEntries = entries.Count,
        entries = entries
    };
});

app.Run();

