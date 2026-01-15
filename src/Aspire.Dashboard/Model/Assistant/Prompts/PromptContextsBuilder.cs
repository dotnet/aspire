// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;

namespace Aspire.Dashboard.Model.Assistant.Prompts;

internal static class PromptContextsBuilder
{
    public static Task ErrorTraces(InitializePromptContext promptContext, string displayText, Func<PagedResult<OtlpTrace>> getErrorTraces)
    {
        var outgoingPeerResolvers = promptContext.ServiceProvider.GetRequiredService<IEnumerable<IOutgoingPeerResolver>>();
        var errorTraces = getErrorTraces();
        foreach (var trace in errorTraces.Items)
        {
            promptContext.DataContext.AddReferencedTrace(trace);
        }

        promptContext.ChatBuilder.AddUserMessage(
            displayText,
            KnownChatMessages.Traces.CreateErrorTracesMessage(errorTraces.Items, outgoingPeerResolvers, promptContext.DashboardOptions).Text);

        return Task.CompletedTask;
    }

    public static Task ErrorStructuredLogs(InitializePromptContext promptContext, string displayText, Func<PagedResult<OtlpLogEntry>> getErrorLogs)
    {
        var errorLogs = getErrorLogs();
        foreach (var log in errorLogs.Items)
        {
            promptContext.DataContext.AddReferencedLogEntry(log);
        }

        promptContext.ChatBuilder.AddUserMessage(
            displayText,
            KnownChatMessages.StructuredLogs.CreateErrorStructuredLogsMessage(errorLogs.Items, promptContext.DashboardOptions).Text);

        return Task.CompletedTask;
    }

    public static Task AnalyzeResource(InitializePromptContext promptContext, string displayText, ResourceViewModel resource)
    {
        promptContext.ChatBuilder.AddUserMessage(
            displayText,
            KnownChatMessages.Resources.CreateAnalyzeResourceMessage(resource).Text);

        return Task.CompletedTask;
    }

    public static Task AnalyzeLogEntry(InitializePromptContext promptContext, string displayText, OtlpLogEntry logEntry)
    {
        promptContext.DataContext.AddReferencedLogEntry(logEntry);
        promptContext.ChatBuilder.AddUserMessage(
            displayText,
            KnownChatMessages.StructuredLogs.CreateAnalyzeLogEntryMessage(logEntry, promptContext.DashboardOptions).Text);

        return Task.CompletedTask;
    }

    public static Task AnalyzeTrace(InitializePromptContext context, string displayText, OtlpTrace trace)
    {
        context.DataContext.AddReferencedTrace(trace);

        var outgoingPeerResolvers = context.ServiceProvider.GetRequiredService<IEnumerable<IOutgoingPeerResolver>>();
        var repository = context.ServiceProvider.GetRequiredService<TelemetryRepository>();
        var traceLogs = repository.GetLogsForTrace(trace.TraceId);
        foreach (var log in traceLogs)
        {
            context.DataContext.AddReferencedLogEntry(log);
        }

        context.ChatBuilder.AddUserMessage(
            displayText,
            KnownChatMessages.Traces.CreateAnalyzeTraceMessage(trace, traceLogs, outgoingPeerResolvers, context.DashboardOptions).Text);

        return Task.CompletedTask;
    }

    public static Task AnalyzeSpan(InitializePromptContext context, string displayText, OtlpSpan span)
    {
        context.DataContext.AddReferencedTrace(span.Trace);

        var outgoingPeerResolvers = context.ServiceProvider.GetRequiredService<IEnumerable<IOutgoingPeerResolver>>();
        var repository = context.ServiceProvider.GetRequiredService<TelemetryRepository>();
        var traceLogs = repository.GetLogsForTrace(span.Trace.TraceId);
        foreach (var log in traceLogs)
        {
            context.DataContext.AddReferencedLogEntry(log);
        }

        context.ChatBuilder.AddUserMessage(
            displayText,
            KnownChatMessages.Traces.CreateAnalyzeSpanMessage(span, traceLogs, outgoingPeerResolvers, context.DashboardOptions).Text);

        return Task.CompletedTask;
    }
}
