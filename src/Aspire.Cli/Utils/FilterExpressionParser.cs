// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Aspire.Cli.Utils;

/// <summary>
/// Represents a parsed filter expression with field, condition, and value components.
/// </summary>
/// <param name="Field">The field name to filter on (e.g., "http.method", "status").</param>
/// <param name="Condition">The filter condition (e.g., Equals, Contains, GreaterThan).</param>
/// <param name="Value">The value to compare against.</param>
public sealed record ParsedFilter(string Field, FilterCondition Condition, string Value)
{
    /// <summary>
    /// Converts this parsed filter to a telemetry filter DTO suitable for JSON serialization
    /// and passing to the Dashboard's MCP tools.
    /// </summary>
    /// <returns>A <see cref="TelemetryFilterDto"/> that can be serialized to JSON.</returns>
    public TelemetryFilterDto ToTelemetryFilter()
    {
        return new TelemetryFilterDto
        {
            Field = Field,
            Condition = Condition.ToTelemetryConditionString(),
            Value = Value
        };
    }
}

/// <summary>
/// A Data Transfer Object representing a telemetry filter that can be serialized to JSON
/// for passing to the Dashboard's MCP tools. This matches the structure expected by
/// <c>FieldTelemetryFilter</c> in the Dashboard.
/// </summary>
public sealed class TelemetryFilterDto
{
    /// <summary>
    /// The field name to filter on.
    /// </summary>
    [JsonPropertyName("field")]
    public required string Field { get; init; }

    /// <summary>
    /// The filter condition as a string (e.g., "equals", "contains", "gt", "gte").
    /// </summary>
    [JsonPropertyName("condition")]
    public required string Condition { get; init; }

    /// <summary>
    /// The value to compare against.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    /// <summary>
    /// Whether the filter is enabled. Defaults to true.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;
}

/// <summary>
/// Represents the filter condition operators.
/// </summary>
public enum FilterCondition
{
    Equals,
    NotEqual,
    Contains,
    NotContains,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual
}

/// <summary>
/// Extension methods for <see cref="FilterCondition"/>.
/// </summary>
public static class FilterConditionExtensions
{
    /// <summary>
    /// Converts the filter condition to the string format expected by the Dashboard's telemetry filtering.
    /// These strings match the format used by <c>TelemetryFilterFormatter</c> in the Dashboard.
    /// </summary>
    /// <param name="condition">The filter condition to convert.</param>
    /// <returns>The string representation for the Dashboard.</returns>
    public static string ToTelemetryConditionString(this FilterCondition condition)
    {
        return condition switch
        {
            FilterCondition.Equals => "equals",
            FilterCondition.NotEqual => "!equals",
            FilterCondition.Contains => "contains",
            FilterCondition.NotContains => "!contains",
            FilterCondition.GreaterThan => "gt",
            FilterCondition.LessThan => "lt",
            FilterCondition.GreaterThanOrEqual => "gte",
            FilterCondition.LessThanOrEqual => "lte",
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, null)
        };
    }
}

/// <summary>
/// Exception thrown when a filter expression cannot be parsed.
/// </summary>
public sealed class FilterParseException : Exception
{
    public FilterParseException(string message) : base(message)
    {
    }

    public FilterParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Parses filter expressions in the format "field{operator}value".
/// Supported operators: =, !=, ~, !~, &gt;, &lt;, &gt;=, &lt;=
/// </summary>
public static partial class FilterExpressionParser
{
    // Regex pattern to match filter expressions
    // Captures: field name, operator, value
    // Operators in order of precedence (longer operators first to avoid partial matches):
    // >=, <=, !=, !~, =, ~, >, <
    // Field group allows zero or more characters (empty field is validated in code)
    [GeneratedRegex(@"^(?<field>[^=!~<>]*?)(?<op>>=|<=|!=|!~|=|~|>|<)(?<value>.*)$", RegexOptions.Compiled)]
    private static partial Regex FilterPattern();

    /// <summary>
    /// Parses a filter expression string into a <see cref="ParsedFilter"/>.
    /// </summary>
    /// <param name="expression">The filter expression to parse (e.g., "http.method=POST", "status!=Error").</param>
    /// <returns>A <see cref="ParsedFilter"/> containing the parsed components.</returns>
    /// <exception cref="FilterParseException">Thrown when the expression is invalid.</exception>
    public static ParsedFilter Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new FilterParseException("Filter expression cannot be empty.");
        }

        var match = FilterPattern().Match(expression);

        if (!match.Success)
        {
            throw new FilterParseException($"Invalid filter expression '{expression}'. Expected format: field{{operator}}value. Supported operators: =, !=, ~, !~, >, <, >=, <=");
        }

        var field = match.Groups["field"].Value.Trim();
        var operatorStr = match.Groups["op"].Value;
        var value = match.Groups["value"].Value;

        if (string.IsNullOrWhiteSpace(field))
        {
            throw new FilterParseException($"Invalid filter expression '{expression}'. Field name is missing.");
        }

        var condition = ParseOperator(operatorStr, expression);

        return new ParsedFilter(field, condition, value);
    }

    /// <summary>
    /// Tries to parse a filter expression string into a <see cref="ParsedFilter"/>.
    /// </summary>
    /// <param name="expression">The filter expression to parse.</param>
    /// <param name="filter">When successful, contains the parsed filter; otherwise, null.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(string expression, out ParsedFilter? filter)
    {
        try
        {
            filter = Parse(expression);
            return true;
        }
        catch (FilterParseException)
        {
            filter = null;
            return false;
        }
    }

    private static FilterCondition ParseOperator(string operatorStr, string expression)
    {
        return operatorStr switch
        {
            "=" => FilterCondition.Equals,
            "!=" => FilterCondition.NotEqual,
            "~" => FilterCondition.Contains,
            "!~" => FilterCondition.NotContains,
            ">" => FilterCondition.GreaterThan,
            "<" => FilterCondition.LessThan,
            ">=" => FilterCondition.GreaterThanOrEqual,
            "<=" => FilterCondition.LessThanOrEqual,
            _ => throw new FilterParseException($"Invalid operator '{operatorStr}' in filter expression '{expression}'. Supported operators: =, !=, ~, !~, >, <, >=, <=")
        };
    }

    /// <summary>
    /// Gets the string representation of a filter condition.
    /// </summary>
    /// <param name="condition">The filter condition.</param>
    /// <returns>The operator string for the condition.</returns>
    public static string ConditionToOperator(FilterCondition condition)
    {
        return condition switch
        {
            FilterCondition.Equals => "=",
            FilterCondition.NotEqual => "!=",
            FilterCondition.Contains => "~",
            FilterCondition.NotContains => "!~",
            FilterCondition.GreaterThan => ">",
            FilterCondition.LessThan => "<",
            FilterCondition.GreaterThanOrEqual => ">=",
            FilterCondition.LessThanOrEqual => "<=",
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, null)
        };
    }

    /// <summary>
    /// Gets a human-readable description of a filter condition.
    /// </summary>
    /// <param name="condition">The filter condition.</param>
    /// <returns>A human-readable description.</returns>
    public static string ConditionToDescription(FilterCondition condition)
    {
        return condition switch
        {
            FilterCondition.Equals => "equals",
            FilterCondition.NotEqual => "not equals",
            FilterCondition.Contains => "contains",
            FilterCondition.NotContains => "not contains",
            FilterCondition.GreaterThan => "greater than",
            FilterCondition.LessThan => "less than",
            FilterCondition.GreaterThanOrEqual => "greater than or equal",
            FilterCondition.LessThanOrEqual => "less than or equal",
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, null)
        };
    }
}
