// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace SqlServerEndToEnd.Common;

public class MyDb1Context(DbContextOptions<MyDb1Context> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }
}
