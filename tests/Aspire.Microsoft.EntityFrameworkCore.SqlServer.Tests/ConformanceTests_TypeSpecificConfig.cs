// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Aspire.Microsoft.Data.SqlClient.Tests;
using Microsoft.Extensions.Configuration;

namespace Aspire.Microsoft.EntityFrameworkCore.SqlServer.Tests;

public class ConformanceTests_TypeSpecificConfig : ConformanceTests
{
    public ConformanceTests_TypeSpecificConfig(SqlServerContainerFixture containerFixture)
        : base(containerFixture)
    {
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new($"Aspire:Microsoft:EntityFrameworkCore:SqlServer:{typeof(TestDbContext).Name}:ConnectionString", ConnectionString)
        });
}
