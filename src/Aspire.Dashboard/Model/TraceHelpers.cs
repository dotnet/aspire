// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Aspire.Dashboard.Model;

public static class TraceHelpers
{
    /// <summary>
    /// Recursively visit spans for a trace. Start visiting spans from unrooted spans.
    /// </summary>
    public static void VisitSpans<TState>(OtlpTrace trace, Func<OtlpSpan, TState, TState> spanAction, TState state)
    {
        // TODO: Investigate performance.
        // A trace's spans are stored in one collection and recursively iterated by matching the span id to its parent.
        // This behavior could cause excessive iteration over the span collection in large traces. Consider improving if this causes performance issues.

        var orderByFunc = static (OtlpSpan s) => s.StartTime;

        foreach (var unrootedSpan in trace.Spans.Where(s => s.GetParentSpan() == null).OrderBy(orderByFunc))
        {
            var newState = spanAction(unrootedSpan, state);

            Visit(trace.Spans, unrootedSpan, spanAction, newState, orderByFunc);
        }

        static void Visit(OtlpSpanCollection allSpans, OtlpSpan span, Func<OtlpSpan, TState, TState> spanAction, TState state, Func<OtlpSpan, DateTime> orderByFunc)
        {
            foreach (var childSpan in OtlpSpan.GetChildSpans(span, allSpans).OrderBy(orderByFunc))
            {
                var newState = spanAction(childSpan, state);

                Visit(allSpans, childSpan, spanAction, newState, orderByFunc);
            }
        }
    }

    private readonly record struct OrderedApplicationsState(DateTime? CurrentMinDate);

    /// <summary>
    /// Get applications for a trace, with grouped information, and ordered using min date.
    /// It is possible for spans to arrive with dates that are out of order (i.e. child span has earlier
    /// start date than the parent) so ensure it isn't possible for a child to appear before parent.
    /// </summary>
    public static IEnumerable<OrderedApplication> GetOrderedApplications(OtlpTrace trace)
    {
        var appFirstTimes = new Dictionary<OtlpApplication, OrderedApplication>();

        VisitSpans(trace, (OtlpSpan span, OrderedApplicationsState state) =>
        {
            var currentMinDate = (state.CurrentMinDate == null || state.CurrentMinDate < span.StartTime)
                ? span.StartTime
                : state.CurrentMinDate.Value;

            ProcessSpanApp(span, span.Source.Application, appFirstTimes, currentMinDate);
            if (span.UninstrumentedPeer is { } peer)
            {
                ProcessSpanApp(span, peer, appFirstTimes, currentMinDate);
            }

            return new OrderedApplicationsState(currentMinDate);
        }, new OrderedApplicationsState(null));

        return appFirstTimes.Select(kvp => kvp.Value)
            .OrderBy(s => s.FirstDateTime)
            .ThenBy(s => s.Index);
    }

    private static void ProcessSpanApp(OtlpSpan span, OtlpApplication application, Dictionary<OtlpApplication, OrderedApplication> appFirstTimes, DateTime currentMinDate)
    {
        if (appFirstTimes.TryGetValue(application, out var orderedApp))
        {
            if (currentMinDate < orderedApp.FirstDateTime)
            {
                orderedApp.FirstDateTime = currentMinDate;
            }

            if (span.Status == OtlpSpanStatusCode.Error)
            {
                orderedApp.ErroredSpans++;
            }

            orderedApp.TotalSpans++;
        }
        else
        {
            appFirstTimes.Add(
                application,
                new OrderedApplication(application, appFirstTimes.Count, currentMinDate, totalSpans: 1, erroredSpans: span.Status == OtlpSpanStatusCode.Error ? 1 : 0));
        }
    }

    private static readonly Icon s_serverFilled = new Icons.Filled.Size16.Server();
    private static readonly Icon s_serverRegular = new Icons.Regular.Size16.Server();

    private static readonly Icon s_mailboxFilled = new Icons.Filled.Size16.Mailbox();
    private static readonly Icon s_mailboxRegular = new Icons.Regular.Size16.Mailbox();

    private static readonly Icon s_contentSettingsFilled = new Icons.Filled.Size16.ContentSettings();
    private static readonly Icon s_contentSettingsRegular = new Icons.Regular.Size16.ContentSettings();

    private static readonly Icon s_mailFilled = new Icons.Filled.Size16.Mail();
    private static readonly Icon s_mailRegular = new Icons.Regular.Size16.Mail();

    private static readonly Icon s_boxFilled = new Icons.Filled.Size16.Box();
    private static readonly Icon s_boxRegular = new Icons.Regular.Size16.Box();

    public static Icon? TryGetSpanIcon(OtlpSpan span, IconVariant iconVariant)
    {
        switch (span.Kind)
        {
            case OtlpSpanKind.Server:
                return GetIcon(s_serverRegular, s_serverFilled, iconVariant);
            case OtlpSpanKind.Consumer:
                if (span.Attributes.HasKey("messaging.system"))
                {
                    return GetIcon(s_mailboxRegular, s_mailboxFilled, iconVariant);
                }
                else
                {
                    return GetIcon(s_contentSettingsRegular, s_contentSettingsFilled, iconVariant);
                }
            case OtlpSpanKind.Producer:
                if (span.Attributes.HasKey("messaging.system"))
                {
                    return GetIcon(s_mailRegular, s_mailFilled, iconVariant);
                }
                else
                {
                    return GetIcon(s_boxRegular, s_boxFilled, iconVariant);
                }
            default:
                return null;
        }

        static Icon GetIcon(Icon regular, Icon filled, IconVariant iconVariant)
        {
            return iconVariant switch
            {
                IconVariant.Filled => filled,
                IconVariant.Regular => regular,
                _ => throw new ArgumentOutOfRangeException(nameof(iconVariant), iconVariant, $"Unsupported icon variant: {iconVariant}")
            };
        }
    }
}

public sealed class OrderedApplication(OtlpApplication application, int index, DateTime firstDateTime, int totalSpans, int erroredSpans)
{
    public OtlpApplication Application { get; } = application;
    public int Index { get; } = index;
    public DateTime FirstDateTime { get; set; } = firstDateTime;
    public int TotalSpans { get; set; } = totalSpans;
    public int ErroredSpans { get; set; } = erroredSpans;
}
