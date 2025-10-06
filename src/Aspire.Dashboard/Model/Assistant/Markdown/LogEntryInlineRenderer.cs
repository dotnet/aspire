// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Aspire.Dashboard.Model.Assistant.Markdown;

public class LogEntryInlineRenderer : HtmlObjectRenderer<LogEntryInline>
{
    protected override void Write(HtmlRenderer renderer, LogEntryInline inline)
    {
        renderer.Write($@"<a href=""{DashboardUrls.StructuredLogsUrl(logEntryId: inline.LogEntry.InternalId)}"" class=""log-entry"">Log {inline.LogEntry.InternalId}</a>");
    }
}
