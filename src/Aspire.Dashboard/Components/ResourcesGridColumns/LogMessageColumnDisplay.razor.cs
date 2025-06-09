// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Components;

public partial class LogMessageColumnDisplay
{
    private string? _exceptionText;

    protected override void OnInitialized()
    {
        _exceptionText = OtlpLogEntry.GetExceptionText(LogEntry);
    }
}
