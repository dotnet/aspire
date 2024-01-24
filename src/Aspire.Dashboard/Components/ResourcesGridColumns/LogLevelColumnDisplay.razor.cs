// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Components;

public partial class LogLevelColumnDisplay
{
    private string? GetErrorInformation()
    {
        if (LogEntry.Properties.GetValue("exception.type") is { } exceptionType)
        {
            var tooltip = new StringBuilder(LogEntry.Message);
            tooltip
                .Append(Environment.NewLine)
                .Append(CultureInfo.CurrentCulture, $"{exceptionType}: {LogEntry.Properties.GetValue("exception.message")}")
                .Append(Environment.NewLine)
                .Append(LogEntry.Properties.GetValue("exception.stacktrace"));
        }

        return null;
    }
}
