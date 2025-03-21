// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var dsBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("db1"));
dsBuilder.UseEntraAuthentication();

builder.AddNpgsqlDbContext<MyDb1Context>("db1",
    configureDbContextOptions: (options) => options.UseNpgsql(dsBuilder.Build()));

var app = builder.Build();

app.MapDefaultEndpoints();
var firstTime = true;
app.MapGet("/", async (MyDb1Context db1Context) =>
{
    if (firstTime)
    {
        firstTime = false;
        db1Context.Database.EnsureCreated();
    }

    // We only work with db1Context for the rest of this
    // since we've proven connectivity to the others for now.
    var entry = new Entry();
    await db1Context.Entries.AddAsync(entry);
    await db1Context.SaveChangesAsync();

    var entries = await db1Context.Entries.ToListAsync();

    return new
    {
        totalEntries = entries.Count,
        entries = entries
    };
});

app.Run();

public class MyDb1Context(DbContextOptions<MyDb1Context> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entry>().HasKey(e => e.Id);
    }

    public DbSet<Entry> Entries { get; set; }
}

public class Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
