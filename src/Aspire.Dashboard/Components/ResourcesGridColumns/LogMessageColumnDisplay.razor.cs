// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Components;

public partial class LogMessageColumnDisplay
{
    private string? _exceptionText;

    protected override void OnInitialized()
    {
        _exceptionText = GetExceptionText();
    }

    private string? GetExceptionText()
    {
        // exception.stacktrace includes the exception message and type.
        // https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/
        if (GetProperty("exception.stacktrace") is { Length: > 0 } stackTrace)
        {
            return stackTrace;
        }

        if (GetProperty("exception.message") is { Length: > 0 } message)
        {
            if (GetProperty("exception.type") is { Length: > 0 } type)
            {
                return $"{type}: {message}";
            }

            return message;
        }

        return null;

        string? GetProperty(string propertyName)
        {
            return LogEntry.Attributes.GetValue(propertyName);
        }
    }
}
