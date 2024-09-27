// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model.Otlp;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class LogFilterFormatterTests
{
    [Fact]
    public void RoundTripFilterWithColon()
    {
        var serializedFilters = LogFilterFormatter.SerializeLogFiltersToString([
            new TelemetryFilter
            {
                Field = "test:name",
                Condition = FilterCondition.Equals,
                Value = "test:value"
            }
        ]);

        var filters = LogFilterFormatter.DeserializeLogFiltersFromString(serializedFilters);

        var filter = Assert.Single(filters);

        Assert.Equal("test:name", filter.Field);
        Assert.Equal("test:value", filter.Value);
    }

    [Fact]
    public void RoundTripFiltersWithPluses()
    {
        var serializedFilters = LogFilterFormatter.SerializeLogFiltersToString([
            new TelemetryFilter
            {
                Field = "test+name",
                Condition = FilterCondition.Equals,
                Value = "test+value"
            }
        ]);

        var filters = LogFilterFormatter.DeserializeLogFiltersFromString(serializedFilters);

        var filter = Assert.Single(filters);

        Assert.Equal("test+name", filter.Field);
        Assert.Equal("test+value", filter.Value);
    }
}
