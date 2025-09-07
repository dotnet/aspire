// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Npgsql.Tests;
using Aspire.TestUtilities;
using Microsoft.Extensions.Configuration;

namespace Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL.Tests;

public class ConformanceTests_TypeSpecificConfig : ConformanceTests
{
    public ConformanceTests_TypeSpecificConfig(PostgreSQLContainerFixture containerFixture) : base(containerFixture)
    {
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new($"Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:{typeof(TestDbContext).Name}:ConnectionString", ConnectionString)
        });
}
