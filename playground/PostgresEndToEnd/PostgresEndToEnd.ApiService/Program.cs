// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("db1")!);
if (dataSourceBuilder.ConnectionStringBuilder.Password is null)
{
    dataSourceBuilder.UsePeriodicPasswordProvider(async (_, ct) =>
    {
        var credentials = new DefaultAzureCredential();
        var token = await credentials.GetTokenAsync(new TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]), ct);
        return token.Token;
    }, TimeSpan.FromHours(24), TimeSpan.FromSeconds(10));
}
var dataSource = dataSourceBuilder.Build();

builder.AddNpgsqlDbContext<MyDb1Context>("db1", configureDbContextOptions: o =>
{
    o.UseNpgsql(dataSource);
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", async (MyDb1Context db1Context) =>
{
    // You wouldn't normally do this on every call,
    // but doing it here just to make this simple.
    await db1Context.Database.EnsureCreatedAsync();

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

public class MyDb2Context(DbContextOptions<MyDb2Context> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entry>().HasKey(e => e.Id);
    }

    public DbSet<Entry> Entries { get; set; }
}

public class MyDb3Context(DbContextOptions<MyDb3Context> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entry>().HasKey(e => e.Id);
    }

    public DbSet<Entry> Entries { get; set; }
}

public class MyDb4Context(DbContextOptions<MyDb4Context> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entry>().HasKey(e => e.Id);
    }

    public DbSet<Entry> Entries { get; set; }
}

public class MyDb5Context(DbContextOptions<MyDb5Context> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entry>().HasKey(e => e.Id);
    }

    public DbSet<Entry> Entries { get; set; }
}

public class MyDb6Context(DbContextOptions<MyDb6Context> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entry>().HasKey(e => e.Id);
    }

    public DbSet<Entry> Entries { get; set; }
}

public class MyDb7Context(DbContextOptions<MyDb7Context> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entry>().HasKey(e => e.Id);
    }

    public DbSet<Entry> Entries { get; set; }
}

public class MyDb8Context(DbContextOptions<MyDb8Context> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entry>().HasKey(e => e.Id);
    }

    public DbSet<Entry> Entries { get; set; }
}

public class MyDb9Context(DbContextOptions<MyDb9Context> options) : DbContext(options)
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
