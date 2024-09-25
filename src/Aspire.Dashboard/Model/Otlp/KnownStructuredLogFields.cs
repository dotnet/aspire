// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.Otlp;

public static class KnownStructuredLogFields
{
    public const string MessageField = "log.message";
    public const string CategoryField = "log.category";
    public const string ApplicationField = "log.application";
    public const string TraceIdField = "log.traceid";
    public const string SpanIdField = "log.spanid";
    public const string OriginalFormatField = "log.originalformat";

    public static readonly List<string> AllFields = [
        MessageField,
        CategoryField,
        ApplicationField,
        TraceIdField,
        SpanIdField,
        OriginalFormatField
    ];
}
