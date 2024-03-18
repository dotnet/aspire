// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Otlp;

namespace Aspire.Dashboard.Extensions;

public static class LogFilterFormatter
{
    private static string SerializeLogFilterToString(LogFilter filter)
    {
        var condition = filter.Condition switch
        {
            FilterCondition.Equals => "equals",
            FilterCondition.Contains => "contains",
            FilterCondition.GreaterThan => "gt",
            FilterCondition.LessThan => "lt",
            FilterCondition.GreaterThanOrEqual => "gte",
            FilterCondition.LessThanOrEqual => "lte",
            FilterCondition.NotEqual => "!equals",
            FilterCondition.NotContains => "!contains",
            _ => null
        };

        return $"{filter.Field}:{condition}:{Uri.EscapeDataString(filter.Value)}";
    }

    public static string SerializeLogFiltersToString(IEnumerable<LogFilter> filters)
    {
        // "%2B" is the escaped form of +
        return string.Join("%2B", filters.Select(SerializeLogFilterToString));
    }

    private static LogFilter? DeserializeLogFilterFromString(string filterString)
    {
        var parts = filterString.Split(':');
        if (parts.Length != 3)
        {
            return null;
        }

        var field = parts[0];

        FilterCondition? condition = parts[1] switch
        {
            "equals" => FilterCondition.Equals,
            "contains" => FilterCondition.Contains,
            "gt" => FilterCondition.GreaterThan,
            "lt" => FilterCondition.LessThan,
            "gte" => FilterCondition.GreaterThanOrEqual,
            "lte" => FilterCondition.LessThanOrEqual,
            "!equals" => FilterCondition.NotEqual,
            "!contains" => FilterCondition.NotContains,
            _ => null
        };

        if (condition is null)
        {
            return null;
        }

        var value = Uri.UnescapeDataString(parts[2]);

        return new LogFilter { Condition = condition.Value, Field = field, Value = value };
    }

    public static List<LogFilter> DeserializeLogFiltersFromString(string filtersString)
    {
        return filtersString
            .Split('+') // + turns into space from query parameter (' ')
            .Select(DeserializeLogFilterFromString)
            .Where(filter => filter is not null)
            .Cast<LogFilter>()
            .ToList();
    }
}
