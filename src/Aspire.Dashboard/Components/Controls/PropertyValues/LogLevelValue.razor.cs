// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Controls.PropertyValues;

public partial class LogLevelValue
{
    [Parameter, EditorRequired]
    public required string Value { get; set; }

    [Parameter, EditorRequired]
    public required string HighlightText { get; set; }

    [Parameter, EditorRequired]
    public required OtlpLogEntry LogEntry { get; set; }
}
