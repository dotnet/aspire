// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Aspire.Dashboard.Model.Otlp;

[DebuggerDisplay("{FilterText,nq}")]
public class LogFilter
{
    public IStringLocalizer<Dialogs> Loc { get; set; } = default!;
    public string Field { get; set; } = default!;
    public FilterCondition Condition { get; set; }
    public string Value { get; set; } = default!;
    public string FilterText => $"{Field} {ConditionToString(Loc, Condition)} {Value}";

    public static List<string> GetAllPropertyNames(IStringLocalizer<Dialogs> loc, List<string> propertyKeys)
    {
        var result = new List<string> { loc[Dialogs.FilterFieldMessage], loc[Dialogs.FilterFieldCategory], loc[Dialogs.FilterFieldApplication], loc[Dialogs.FilterFieldTraceId], loc[Dialogs.FilterFieldSpanId], loc[Dialogs.FilterFieldOriginalFormat] };
        result.AddRange(propertyKeys);
        return result;
    }

    public static string ConditionToString(IStringLocalizer<Dialogs> loc, FilterCondition c) =>
        c switch
        {
            FilterCondition.Equals => "==",
            FilterCondition.Contains => loc[Dialogs.FilterConditionContains],
            FilterCondition.GreaterThan => ">",
            FilterCondition.LessThan => "<",
            FilterCondition.GreaterThanOrEqual => ">=",
            FilterCondition.LessThanOrEqual => "<=",
            FilterCondition.NotEqual => "!=",
            FilterCondition.NotContains => loc[Dialogs.FilterConditionNotContains],
            _ => throw new ArgumentOutOfRangeException(nameof(c), c, null)
        };

    private static Func<string, string, bool> ConditionToFuncString(FilterCondition c) =>
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
        if (Field == Loc[Dialogs.FilterFieldMessage])
        {
            return x.Message;
        }
        if (Field == Loc[Dialogs.FilterFieldApplication])
        {
            return x.Application.ApplicationName;
        }
        if (Field == Loc[Dialogs.FilterFieldTraceId])
        {
            return x.TraceId;
        }
        if (Field == Loc[Dialogs.FilterFieldSpanId])
        {
            return x.SpanId;
        }
        if (Field == Loc[Dialogs.FilterFieldOriginalFormat])
        {
            return x.OriginalFormat;
        }
        if (Field == Loc[Dialogs.FilterFieldCategory])
        {
            return x.Scope.ScopeName;
        }
        return x.Properties.GetValue(Field);
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
                    var func = ConditionToFuncNumber(Condition);
                    if (Enum.TryParse<LogLevel>(Value, true, out var value))
                    {
                        return input.Where(x => func((int)x.Severity, (double)value));
                    }
                    return input;
                }
            default:
                {
                    return input.Where(x => ConditionToFuncString(Condition)(GetFieldValue(x)!, Value));
                }
        }
    }
}
