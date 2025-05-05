// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

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
}

public sealed class OrderedApplication(OtlpApplication application, int index, DateTime firstDateTime, int totalSpans, int erroredSpans)
{
    public OtlpApplication Application { get; } = application;
    public int Index { get; } = index;
    public DateTime FirstDateTime { get; set; } = firstDateTime;
    public int TotalSpans { get; set; } = totalSpans;
    public int ErroredSpans { get; set; } = erroredSpans;
}
