// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Otlp;
using Aspire.Hosting.Utils;

namespace Aspire.Dashboard.Extensions;

public static class TelemetryFilterFormatter
{
    private const string DisabledText = "disabled";

    private static string SerializeFilterToString(TelemetryFilter filter)
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

        var filterString = $"{StringUtils.Escape(filter.Field)}:{condition}:{StringUtils.Escape(filter.Value)}";
        if (!filter.Enabled)
        {
            filterString += $":{DisabledText}";
        }

        return filterString;
    }

    public static string SerializeFiltersToString(IEnumerable<TelemetryFilter> filters)
    {
        return string.Join(" ", filters.Select(SerializeFilterToString));
    }

    private static TelemetryFilter? DeserializeFilterFromString(string filterString)
    {
        var parts = filterString.Split(':');
        if (parts.Length != 3 && parts.Length != 4)
        {
            return null;
        }

        var field = StringUtils.Unescape(parts[0]);

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

        var value = StringUtils.Unescape(parts[2]);

        var enabled = parts is not [_, _, _, DisabledText];

        return new TelemetryFilter
        {
            Condition = condition.Value,
            Field = field,
            Value = value,
            Enabled = enabled
        };
    }

    public static List<TelemetryFilter> DeserializeFiltersFromString(string filtersString)
    {
        return filtersString
            .Split(' ')
            .Select(DeserializeFilterFromString)
            .Where(filter => filter is not null)
            .Cast<TelemetryFilter>()
            .ToList();
    }
}
