// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<MyDb1Context>("db1");
builder.AddSqlServerDbContext<MyDb2Context>("db2");

var app = builder.Build();

app.MapGet("/", async (MyDb1Context db1Context, MyDb2Context db2Context) =>
{
    // You wouldn't normally do this on every call,
    // but doing it here just to make this simple.

    await db1Context.Database.EnsureCreatedAsync();
    await db2Context.Database.EnsureCreatedAsync();

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
    public DbSet<Entry> Entries { get; set; }
}

public class MyDb2Context(DbContextOptions<MyDb2Context> options) : DbContext(options)
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
