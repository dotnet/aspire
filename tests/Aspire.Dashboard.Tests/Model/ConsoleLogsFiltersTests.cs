// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Dashboard.Model;
using Xunit;

namespace Aspire.Dashboard.Tests.Model;

public sealed class ConsoleLogsFiltersTests
{
    [Fact]
    public void Serialize()
    {
        // Arrange
        var filters = new ConsoleLogsFilters
        {
            FilterAllLogsDate = new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            FilterResourceLogsDates =
            {
                ["test-abc"] = new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc)
            }
        };

        // Act
        var json = JsonSerializer.Serialize(filters);
        var deserialized = JsonSerializer.Deserialize<ConsoleLogsFilters>(json)!;

        // Assert
        Assert.Equal(filters.FilterAllLogsDate, deserialized.FilterAllLogsDate);
        Assert.Equivalent(filters.FilterResourceLogsDates, deserialized.FilterResourceLogsDates);
    }
}
