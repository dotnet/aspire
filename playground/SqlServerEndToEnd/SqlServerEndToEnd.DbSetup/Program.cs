// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using SqlServerEndToEnd.Common;

var builder = WebApplication.CreateBuilder(args);
builder.AddSqlServerDbContext<MyDb1Context>("db1");
builder.AddSqlServerDbContext<MyDb2Context>("db2");
using var app = builder.Build();
using var scope = app.Services.CreateScope();
using var db1 = scope.ServiceProvider.GetRequiredService<MyDb1Context>();
using var db2 = scope.ServiceProvider.GetRequiredService<MyDb2Context>();

foreach (var db in new DbContext[] { db1, db2 })
{
    var created = await db.Database.EnsureCreatedAsync();
    if (created)
    {
        Console.WriteLine("Database schema created!");
    }
}
