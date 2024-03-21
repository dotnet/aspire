// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model.Otlp;

[DebuggerDisplay("{FilterText,nq}")]
public class LogFilter
{
    public string Field { get; set; } = default!;
    public FilterCondition Condition { get; set; }
    public string Value { get; set; } = default!;

    public string FilterText => $"{Field} {ConditionToString(Condition)} {Value}";

    public static List<string> GetAllPropertyNames(List<string> propertyKeys)
    {
        var result = new List<string> { "Message", "Category", "Application", "TraceId", "SpanId", "OriginalFormat" };
        result.AddRange(propertyKeys);
        return result;
    }

    public static string ConditionToString(FilterCondition c) =>
        c switch
        {
            FilterCondition.Equals => "==",
            FilterCondition.Contains => "contains",
            FilterCondition.GreaterThan => ">",
            FilterCondition.LessThan => "<",
            FilterCondition.GreaterThanOrEqual => ">=",
            FilterCondition.LessThanOrEqual => "<=",
            FilterCondition.NotEqual => "!=",
            FilterCondition.NotContains => "not contains",
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static Func<string?, string, bool> ConditionToFuncString(FilterCondition c) =>
        c switch
        {
            FilterCondition.Equals => (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase),
            FilterCondition.Contains => (a, b) => a != null && a.Contains(b, StringComparison.OrdinalIgnoreCase),
            // Condition.GreaterThan => (a, b) => a > b,
            // Condition.LessThan => (a, b) => a < b,
            // Condition.GreaterThanOrEqual => (a, b) => a >= b,
            // Condition.LessThanOrEqual => (a, b) => a <= b,
            FilterCondition.NotEqual => (a, b) => !string.Equals(a, b, StringComparison.OrdinalIgnoreCase),
            FilterCondition.NotContains => (a, b) => a != null && !a.Contains(b, StringComparison.OrdinalIgnoreCase),
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static Func<DateTime, DateTime, bool> ConditionToFuncDate(FilterCondition c) =>
        c switch
        {
            FilterCondition.Equals => (a, b) => a == b,
            //Condition.Contains => (a, b) => a.Contains(b),
            FilterCondition.GreaterThan => (a, b) => a > b,
            FilterCondition.LessThan => (a, b) => a < b,
            FilterCondition.GreaterThanOrEqual => (a, b) => a >= b,
            FilterCondition.LessThanOrEqual => (a, b) => a <= b,
            FilterCondition.NotEqual => (a, b) => a != b,
            //Condition.NotContains => (a, b) => !a.Contains(b),
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static Func<double, double, bool> ConditionToFuncNumber(FilterCondition c) =>
        c switch
        {
            FilterCondition.Equals => (a, b) => a == b,
            //Condition.Contains => (a, b) => a.Contains(b),
            FilterCondition.GreaterThan => (a, b) => a > b,
            FilterCondition.LessThan => (a, b) => a < b,
            FilterCondition.GreaterThanOrEqual => (a, b) => a >= b,
            FilterCondition.LessThanOrEqual => (a, b) => a <= b,
            FilterCondition.NotEqual => (a, b) => a != b,
            //Condition.NotContains => (a, b) => !a.Contains(b),
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private string? GetFieldValue(OtlpLogEntry x)
    {
        return Field switch
        {
            "Message" => x.Message,
            "Application" => x.Application.ApplicationName,
            "TraceId" => x.TraceId,
            "SpanId" => x.SpanId,
            "OriginalFormat" => x.OriginalFormat,
            "Category" => x.Scope.ScopeName,
            _ => x.Attributes.GetValue(Field)
        };
    }

    public IEnumerable<OtlpLogEntry> Apply(IEnumerable<OtlpLogEntry> input)
    {
        switch (Field)
        {
            case nameof(OtlpLogEntry.TimeStamp):
                {
                    var date = DateTime.Parse(Value, CultureInfo.InvariantCulture);
                    var func = ConditionToFuncDate(Condition);
                    return input.Where(x => func(x.TimeStamp, date));
                }
            case nameof(OtlpLogEntry.Severity):
                {
                    if (Enum.TryParse<LogLevel>(Value, ignoreCase: true, out var value))
                    {
                        var func = ConditionToFuncNumber(Condition);
                        return input.Where(x => func((int)x.Severity, (double)value));
                    }
                    return input;
                }
            default:
                {
                    var func = ConditionToFuncString(Condition);
                    return input.Where(x => func(GetFieldValue(x), Value));
                }
        }
    }
}
