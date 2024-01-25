// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Components;

public partial class LogLevelColumnDisplay
{
    private bool TryGetErrorInformation([MaybeNullWhen(false)] out string type, [MaybeNullWhen(false)] out string message, [MaybeNullWhen(false)] out string stackTrace)
    {
        if (LogEntry.Properties.GetValue("ex.Type") is { } exceptionType)
        {
            type = exceptionType;
            message = LogEntry.Properties.GetValue("ex.Message");
            stackTrace = LogEntry.Properties.GetValue("ex.StackTrace");

            Debug.Assert(message is not null);
            Debug.Assert(stackTrace is not null);

            return true;
        }

        type = null;
        message = null;
        stackTrace = null;
        return false;
    }
}
