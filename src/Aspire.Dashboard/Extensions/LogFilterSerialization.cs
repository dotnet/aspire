// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Web;
using Aspire.Dashboard.Model.Otlp;

namespace Aspire.Dashboard.Extensions;

public class LogFilterSerialization
{
    private static string SerializeLogFilterToString(LogFilter filter)
    {
        var condition = LogFilter.ConditionToString(filter.Condition);

        return $"{filter.Field}:{condition}:{HttpUtility.UrlEncode(filter.Value)}";
    }

    public static string SerializeLogFiltersToString(IEnumerable<LogFilter> filters)
    {
        return string.Join('+', filters.Select(SerializeLogFilterToString));
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
            "==" => FilterCondition.Equals,
            "contains" => FilterCondition.Contains,
            ">" => FilterCondition.GreaterThan,
            "<" => FilterCondition.LessThan,
            ">=" => FilterCondition.GreaterThanOrEqual,
            "<=" => FilterCondition.LessThanOrEqual,
            "!=" => FilterCondition.NotEqual,
            "not contains" => FilterCondition.NotContains,
            _ => null
        };

        if (condition is null)
        {
            return null;
        }

        var value = HttpUtility.UrlDecode(parts[2]);

        return new LogFilter { Condition = condition.Value, Field = field, Value = value };
    }

    public static List<LogFilter> DeserializeLogFiltersFromString(string filtersString)
    {
        return filtersString
            .Split('+')
            .Select(DeserializeLogFilterFromString)
            .Where(filter => filter is not null)
            .Cast<LogFilter>()
            .ToList();
    }
}
