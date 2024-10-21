// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("{DebuggerToString(),nq}")]
public class OtlpTrace
{
    private OtlpSpan? _rootSpan;

    public ReadOnlyMemory<byte> Key { get; }
    public string TraceId { get; }

    public string FullName { get; private set; }
    public OtlpSpan FirstSpan => Spans[0]; // There should always be at least one span in a trace.
    public DateTime TimeStamp => FirstSpan.StartTime;
    public OtlpSpan? RootSpan => _rootSpan;
    public TimeSpan Duration
    {
        get
        {
            var start = FirstSpan.StartTime;
            DateTime end = default;
            foreach (var span in Spans)
            {
                if (span.EndTime > end)
                {
                    end = span.EndTime;
                }
            }
            return end - start;
        }
    }

    public List<OtlpSpan> Spans { get; } = new List<OtlpSpan>();

    public int CalculateDepth(OtlpSpan span)
    {
        var depth = 0;
        var currentSpan = span;
        while (currentSpan != null)
        {
            depth++;
            currentSpan = currentSpan.GetParentSpan();
        }
        return depth;
    }

    public int CalculateMaxDepth() => Spans.Max(CalculateDepth);

    public void AddSpan(OtlpSpan span)
    {
        var added = false;
        for (var i = Spans.Count - 1; i >= 0; i--)
        {
            if (span.StartTime > Spans[i].StartTime)
            {
                Spans.Insert(i + 1, span);
                added = true;
                break;
            }
        }
        if (!added)
        {
            Spans.Insert(0, span);
        }

        if (HasCircularReference(span))
        {
            Spans.Remove(span);
            throw new InvalidOperationException($"Circular loop detected for span '{span.SpanId}'.");
        }

        if (string.IsNullOrEmpty(span.ParentSpanId))
        {
            // There should only be one span with no parent span ID.
            // Incase there isn't, the first span with no parent span ID is considered to be the root.
            foreach (var existingSpan in Spans)
            {
                if (string.IsNullOrEmpty(existingSpan.ParentSpanId))
                {
                    _rootSpan = existingSpan;
                    FullName = BuildFullName(existingSpan);
                    break;
                }
            }
        }
        else if (_rootSpan == null && span == Spans[0])
        {
            // If there isn't a root span then the first span is used as the trace name.
            FullName = BuildFullName(span);
        }

        AssertSpanOrder();

        static string BuildFullName(OtlpSpan existingSpan)
        {
            return $"{existingSpan.Source.Application.ApplicationName}: {existingSpan.Name}";
        }
    }

    private static bool HasCircularReference(OtlpSpan span)
    {
        // Can't have a circular reference if the span has no parent.
        if (string.IsNullOrEmpty(span.ParentSpanId))
        {
            return false;
        }

        // Walk up span ancestors to check there is no loop.
        var stack = new List<OtlpSpan> { span };
        var currentSpan = span;
        while (currentSpan.GetParentSpan() is { } parentSpan)
        {
            if (stack.Contains(parentSpan))
            {
                return true;
            }

            stack.Add(parentSpan);
            currentSpan = parentSpan;
        }

        return false;
    }

    [Conditional("DEBUG")]
    private void AssertSpanOrder()
    {
        DateTime current = default;
        for (var i = 0; i < Spans.Count; i++)
        {
            var span = Spans[i];
            if (span.StartTime < current)
            {
                throw new InvalidOperationException($"Trace {TraceId} spans not in order at index {i}.");
            }

            current = span.StartTime;
        }
    }

    public OtlpTrace(ReadOnlyMemory<byte> traceId)
    {
        Key = traceId;
        TraceId = OtlpHelpers.ToHexString(traceId);
        FullName = string.Empty;
    }

    public static OtlpTrace Clone(OtlpTrace trace)
    {
        var newTrace = new OtlpTrace(trace.Key);
        foreach (var item in trace.Spans)
        {
            newTrace.AddSpan(OtlpSpan.Clone(item, newTrace));
        }

        return newTrace;
    }

    private string DebuggerToString()
    {
        return $@"TraceId = ""{TraceId}"", Spans = {Spans.Count}, StartDate = {FirstSpan.StartTime.ToLocalTime():yyyy:MM:dd}, StartTime = {FirstSpan.StartTime.ToLocalTime():h:mm:ss.fff tt}, Duration = {Duration}";
    }

    private sealed class SpanStartDateComparer : IComparer<OtlpSpan>
    {
        public static readonly SpanStartDateComparer Instance = new SpanStartDateComparer();

        public int Compare(OtlpSpan? x, OtlpSpan? y)
        {
            return x!.StartTime.CompareTo(y!.StartTime);
        }
    }
}
