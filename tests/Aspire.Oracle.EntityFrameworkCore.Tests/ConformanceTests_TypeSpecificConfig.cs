// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;

public class ConformanceTests_TypeSpecificConfig : ConformanceTests
{
    public ConformanceTests_TypeSpecificConfig(OracleContainerFixture containerFixture, ITestOutputHelper testOutputHelper) : base(containerFixture, testOutputHelper)
    {
    }

    protected override void PopulateConfiguration(ConfigurationManager configuration, string? key = null)
        => configuration.AddInMemoryCollection(new KeyValuePair<string, string?>[1]
        {
            new($"Aspire:Oracle:EntityFrameworkCore:{typeof(TestDbContext).Name}:ConnectionString", ConnectionString)
        });
}
