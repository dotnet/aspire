// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace Aspire.Components.Common.Tests;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
        Options = options;
    }

    public DbContextOptions<TestDbContext> Options { get; }

    public DbSet<CatalogBrand> CatalogBrands => Set<CatalogBrand>();

    public class CatalogBrand
    {
        public int Id { get; set; }
        public string Brand { get; set; } = default!;
    }
}
