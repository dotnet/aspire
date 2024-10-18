// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model;

public static class TraceHelpers
{
    public sealed class OrderedApplication(OtlpApplication application, int index, DateTime firstDateTime, int totalSpans, int erroredSpans)
    {
        public OtlpApplication Application { get; } = application;
        public int Index { get; } = index;
        public DateTime FirstDateTime { get; set; } = firstDateTime;
        public int TotalSpans { get; set; } = totalSpans;
        public int ErroredSpans { get; set; } = erroredSpans;
    }

    /// <summary>
    /// Get applications for a trace, with grouped information, and ordered using min date.
    /// It is possible for spans to arrive with dates that are out of order (i.e. child span has earlier
    /// start date than the parent) so ensure it isn't possible for a child to appear before parent.
    /// </summary>
    public static IEnumerable<OrderedApplication> GetOrderedApplications(OtlpTrace trace)
    {
        var appFirstTimes = new Dictionary<OtlpApplication, OrderedApplication>();

        // Start from the unparented spans and visit children.
        foreach (var item in trace.Spans.Where(s => s.GetParentSpan() == null))
        {
            Visit(appFirstTimes, currentMinDate: null, item);
        }

        return appFirstTimes.Select(kvp => kvp.Value)
            .OrderBy(s => s.FirstDateTime)
            .ThenBy(s => s.Index);

        static void Visit(Dictionary<OtlpApplication, OrderedApplication> appFirstTimes, DateTime? currentMinDate, OtlpSpan span)
        {
            if (currentMinDate == null || currentMinDate < span.StartTime)
            {
                currentMinDate = span.StartTime;
            }

            if (appFirstTimes.TryGetValue(span.Source.Application, out var orderedApp))
            {
                if (currentMinDate < orderedApp.FirstDateTime)
                {
                    orderedApp.FirstDateTime = currentMinDate.Value;
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
                    span.Source.Application,
                    new OrderedApplication(span.Source.Application, appFirstTimes.Count, currentMinDate.Value, totalSpans: 1, erroredSpans: span.Status == OtlpSpanStatusCode.Error ? 1 : 0));
            }

            foreach (var childSpan in span.GetChildSpans())
            {
                Visit(appFirstTimes, currentMinDate.Value, childSpan);
            }
        }
    }
}
