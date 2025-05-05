// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace Publishers.Common;

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Entry>().HasKey(e => e.Id);
    }

    public DbSet<Entry> Entries { get; set; }
}
