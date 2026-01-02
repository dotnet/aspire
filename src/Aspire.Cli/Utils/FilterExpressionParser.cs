// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Cli.Utils;

/// <summary>
/// Represents a parsed filter expression with field, condition, and value components.
/// </summary>
/// <param name="Field">The field name to filter on (e.g., "http.method", "status").</param>
/// <param name="Condition">The filter condition (e.g., Equals, Contains, GreaterThan).</param>
/// <param name="Value">The value to compare against.</param>
public sealed record ParsedFilter(string Field, FilterCondition Condition, string Value);

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
