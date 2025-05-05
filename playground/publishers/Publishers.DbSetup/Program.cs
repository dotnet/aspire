// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Publishers.Common;

var builder = WebApplication.CreateBuilder(args);
builder.AddNpgsqlDbContext<MyDbContext>("db");
using var app = builder.Build();
using var scope = app.Services.CreateScope();
using var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();

var created = await db.Database.EnsureCreatedAsync();
if (created)
{
    Console.WriteLine("Database created!");
}
