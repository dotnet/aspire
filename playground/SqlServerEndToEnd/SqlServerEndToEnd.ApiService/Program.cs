// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<MyDb1Context>("db1");
builder.AddSqlServerDbContext<MyDb2Context>("db2");
builder.AddSqlServerClient("db3");

var app = builder.Build();

app.MapGet("/", async (MyDb1Context db1Context, MyDb2Context db2Context, SqlConnection db3Connection, CancellationToken cancellationToken) =>
{
    // You wouldn't normally do this on every call,
    // but doing it here just to make this simple.

    await db1Context.Database.EnsureCreatedAsync(cancellationToken);
    var entry = new Entry();
    await db1Context.Entries.AddAsync(entry, cancellationToken);
    await db1Context.SaveChangesAsync(cancellationToken);

    var entries = await db1Context.Entries.ToListAsync(cancellationToken);

    await db2Context.Database.EnsureCreatedAsync(cancellationToken);

    await db3Connection.OpenWithCreateAsync(cancellationToken);
    var command = db3Connection.CreateCommand();
    command.CommandText = "SELECT DB_NAME();";
    var dbName = (string?)await command.ExecuteScalarAsync(cancellationToken);

    return new
    {
        totalEntries = entries.Count,
        entries = entries,
        dbName = dbName
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
