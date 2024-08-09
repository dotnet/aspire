// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

public class EFCoreCosmosDbContext(DbContextOptions<EFCoreCosmosDbContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }
}

public record Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
