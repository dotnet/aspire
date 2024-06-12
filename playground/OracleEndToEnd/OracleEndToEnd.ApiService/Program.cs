// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddOracleDatabaseDbContext<MyDbContext>("FREEPDB1");

var app = builder.Build();

app.MapGet("/", async (MyDbContext dbContext) =>
{
    var users = await dbContext.Users.ToListAsync();

    return new
    {
        totalEntries = users.Count,
        users
    };
});

app.Run();

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasKey(e => e.UserName);
    }

    public DbSet<User> Users { get; set; }
}

[Table("ALL_USERS")]
public class User
{
    [Column("USERNAME")]
    public string? UserName { get; set; }
}
