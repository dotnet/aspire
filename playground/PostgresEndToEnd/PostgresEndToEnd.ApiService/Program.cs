// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<MyDb1Context>("db1");
builder.AddNpgsqlDbContext<MyDb2Context>("db2");
builder.AddNpgsqlDbContext<MyDb3Context>("db3");
builder.AddNpgsqlDbContext<MyDb4Context>("db4");
builder.AddNpgsqlDbContext<MyDb5Context>("db5");
builder.AddNpgsqlDbContext<MyDb6Context>("db6");
builder.AddNpgsqlDbContext<MyDb7Context>("db7");

var connectionString = builder.Configuration.GetConnectionString("db8");
builder.Services.AddDbContextPool<MyDb8Context>(dbContextOptionsBuilder => dbContextOptionsBuilder.UseNpgsql(connectionString));
builder.EnrichNpgsqlDbContext<MyDb8Context>();

builder.AddNpgsqlDbContext<MyDb9Context>("db9");

var app = builder.Build();

app.MapGet("/", async (MyDb1Context db1Context, MyDb2Context db2Context, MyDb3Context db3Context, MyDb4Context db4Context, MyDb5Context db5Context, MyDb6Context db6Context, MyDb7Context db7Context, MyDb8Context db8Context, MyDb9Context db9Context) =>
{
    // You wouldn't normally do this on every call,
    // but doing it here just to make this simple.
    db1Context.Database.EnsureCreated();
    db2Context.Database.EnsureCreated();
    db3Context.Database.EnsureCreated();
    db4Context.Database.EnsureCreated();
    db5Context.Database.EnsureCreated();
    db6Context.Database.EnsureCreated();
    db7Context.Database.EnsureCreated();
    db8Context.Database.EnsureCreated();
    db9Context.Database.EnsureCreated();

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
