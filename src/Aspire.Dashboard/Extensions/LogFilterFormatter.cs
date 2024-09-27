// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Otlp;

namespace Aspire.Dashboard.Extensions;

public static class LogFilterFormatter
{
    private static string SerializeLogFilterToString(TelemetryFilter filter)
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

        return $"{Escape(filter.Field)}:{condition}:{Escape(filter.Value)}";
    }

    public static string SerializeLogFiltersToString(IEnumerable<TelemetryFilter> filters)
    {
        // "%2B" is the escaped form of +
        return string.Join("%2B", filters.Select(SerializeLogFilterToString));
    }

    private static TelemetryFilter? DeserializeLogFilterFromString(string filterString)
    {
        var parts = filterString.Split(':');
        if (parts.Length != 3)
        {
            return null;
        }

        var field = Unescape(parts[0]);

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

        var value = Unescape(parts[2]);

        return new TelemetryFilter
        {
            Condition = condition.Value,
            Field = field,
            Value = value
        };
    }

    private static string Escape(string value)
    {
        // Blazor unescapes the querystring before giving it to the app. Double encode significant characters.
        return Uri.EscapeDataString(value.Replace(":", "%3A").Replace("+", "%2B"));
    }

    private static string Unescape(string value)
    {
        // Blazor unescapes the querystring before giving it to the app. Double decode significant characters.
        return Uri.UnescapeDataString(value).Replace("%3A", ":").Replace("%2B", "+");
    }

    public static List<TelemetryFilter> DeserializeLogFiltersFromString(string filtersString)
    {
        return filtersString
            .Split('+') // + turns into space from query parameter (' ')
            .Select(DeserializeLogFilterFromString)
            .Where(filter => filter is not null)
            .Cast<TelemetryFilter>()
            .ToList();
    }
}
