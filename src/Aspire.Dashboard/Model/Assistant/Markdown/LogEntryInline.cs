// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Markdig.Syntax.Inlines;

namespace Aspire.Dashboard.Model.Assistant.Markdown;

public class LogEntryInline : LeafInline
{
    public required OtlpLogEntry LogEntry { get; init; }
}
