// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace Aspire.Hosting.SqlServer.Tests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
        Options = options;
    }

    public DbContextOptions<TestDbContext> Options { get; }

    public DbSet<Car> Cars => Set<Car>();

    public class Car
    {
        public int Id { get; set; }
        public required string Brand { get; set; }
    }
}
