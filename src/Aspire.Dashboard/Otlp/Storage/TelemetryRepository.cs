// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Otlp.Model;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;
using static OpenTelemetry.Proto.Trace.V1.Span.Types;

namespace Aspire.Dashboard.Otlp.Storage;

public class TelemetryRepository
{
    private int MaxOperationCount { get; init; }
    private int MaxLogCount { get; init; }

    private readonly object _lock = new();
    private readonly ILogger _logger;

    private readonly List<Subscription> _applicationSubscriptions = new();
    private readonly List<Subscription> _logSubscriptions = new();
    private readonly List<Subscription> _metricsSubscriptions = new();
    private readonly List<Subscription> _tracesSubscriptions = new();

    private readonly ConcurrentDictionary<string, OtlpApplication> _applications = new();

    private readonly ReaderWriterLockSlim _logsLock = new();
    private readonly List<OtlpLogEntry> _logs = new();
    private readonly HashSet<(OtlpApplication Application, string PropertyKey)> _logPropertyKeys = new();

    private readonly ReaderWriterLockSlim _tracesLock = new();
    private readonly Dictionary<string, OtlpTraceScope> _traceScopes = new();
    private readonly OtlpTraceCollection _traces = new();

    public TelemetryRepository(IConfiguration config, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(typeof(TelemetryRepository));
        MaxOperationCount = config.GetValue(nameof(MaxOperationCount), 128);
        MaxLogCount = config.GetValue(nameof(MaxLogCount), 4096);
    }

    public List<OtlpApplication> GetApplications()
    {
        var applications = new List<OtlpApplication>();
        foreach (var kvp in _applications)
        {
            applications.Add(kvp.Value);
        }
        applications.Sort((a, b) => string.Compare(a.ApplicationName, b.ApplicationName, StringComparison.OrdinalIgnoreCase));
        return applications;
    }

    public OtlpApplication GetOrAddApplication(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var serviceInstanceId = resource.GetServiceId();
        if (serviceInstanceId is null)
        {
            throw new InvalidOperationException($"Resource does not have a '{OtlpApplication.SERVICE_INSTANCE_ID}' attribute.");
        }

        // Fast path.
        if (_applications.TryGetValue(serviceInstanceId, out var application))
        {
            return application;
        }

        // Slower get or add path.
        (application, var isNew) = GetOrAddApplication(serviceInstanceId, resource);
        if (isNew)
        {
            RaiseSubscriptionChanged(_applicationSubscriptions);
        }

        return application;

        (OtlpApplication, bool) GetOrAddApplication(string serviceId, Resource resource)
        {
            // This GetOrAdd allocates a closure, so we avoid it if possible.
            var newApplication = false;
            var application = _applications.GetOrAdd(serviceId, _ =>
            {
                newApplication = true;
                return new OtlpApplication(resource, _applications, _logger);
            });
            return (application, newApplication);
        }
    }

    public Subscription OnNewApplications(Func<Task> callback)
    {
        return AddSubscription(string.Empty, callback, _applicationSubscriptions);
    }

    public Subscription OnNewLogs(string? applicationId, Func<Task> callback)
    {
        return AddSubscription(applicationId, callback, _logSubscriptions);
    }

    public Subscription OnNewMetrics(string? applicationId, Func<Task> callback)
    {
        return AddSubscription(applicationId, callback, _metricsSubscriptions);
    }

    public Subscription OnNewTraces(string? applicationId, Func<Task> callback)
    {
        return AddSubscription(applicationId, callback, _tracesSubscriptions);
    }

    private Subscription AddSubscription(string? applicationId, Func<Task> callback, List<Subscription> subscriptions)
    {
        Subscription? subscription = null;
        subscription = new Subscription(applicationId, callback, () =>
        {
            lock (_lock)
            {
                subscriptions.Remove(subscription!);
            }
        });

        lock (_lock)
        {
            subscriptions.Add(subscription);
        }

        return subscription;
    }

    private void RaiseSubscriptionChanged(List<Subscription> subscriptions)
    {
        lock (_lock)
        {
            foreach (var subscription in subscriptions)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await subscription.ExecuteAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in subscription callback");
                    }
                });
            }
        }
    }

    public void AddLogs(AddContext context, RepeatedField<ResourceLogs> resourceLogs)
    {
        foreach (var rl in resourceLogs)
        {
            OtlpApplication application;
            try
            {
                application = GetOrAddApplication(rl.Resource);
            }
            catch (Exception ex)
            {
                context.FailureCount += rl.ScopeLogs.Count;
                _logger.LogInformation(ex, "Error adding application.");
                continue;
            }

            AddLogsCore(context, application, rl.ScopeLogs);
        }

        RaiseSubscriptionChanged(_logSubscriptions);
    }

    public void AddLogsCore(AddContext context, OtlpApplication application, RepeatedField<ScopeLogs> scopeLogs)
    {
        _logsLock.EnterWriteLock();

        try
        {
            foreach (var sl in scopeLogs)
            {
                // Instrumentation Scope isn't commonly used for logs.
                // Skip it for now until there is feedback that it has useful information.

                foreach (var record in sl.LogRecords)
                {
                    try
                    {
                        var logEntry = new OtlpLogEntry(record, application);

                        // Insert log entry in the correct position based on timestamp.
                        // Logs can be added out of order by different services.
                        var added = false;
                        for (var i = _logs.Count - 1; i >= 0; i--)
                        {
                            if (logEntry.TimeStamp > _logs[i].TimeStamp)
                            {
                                _logs.Insert(i + 1, logEntry);
                                added = true;
                                break;
                            }
                        }
                        if (!added)
                        {
                            _logs.Insert(0, logEntry);
                        }

                        foreach (var kvp in logEntry.Properties)
                        {
                            _logPropertyKeys.Add((application, kvp.Key));
                        }
                    }
                    catch (Exception ex)
                    {
                        context.FailureCount++;
                        _logger.LogInformation(ex, "Error adding log entry.");
                    }
                }
            }
        }
        finally
        {
            _logsLock.ExitWriteLock();
        }
    }

    public PagedResult<OtlpLogEntry> GetLogs(GetLogsContext context)
    {
        OtlpApplication? application = null;
        if (context.ApplicationServiceId != null && !_applications.TryGetValue(context.ApplicationServiceId, out application))
        {
            return PagedResult<OtlpLogEntry>.Empty;
        }

        _logsLock.EnterReadLock();

        try
        {
            var results = _logs.AsEnumerable();
            if (application != null)
            {
                results = results.Where(l => l.Application == application);
            }

            foreach (var filter in context.Filters)
            {
                results = filter.Apply(results);
            }

            return OtlpHelpers.GetItems(results, context.StartIndex, context.Count);
        }
        finally
        {
            _logsLock.ExitReadLock();
        }
    }

    public List<string> GetLogPropertyKeys(string? applicationServiceId)
    {
        _logsLock.EnterReadLock();

        try
        {
            var applicationKeys = _logPropertyKeys.AsEnumerable();
            if (applicationServiceId != null)
            {
                applicationKeys = applicationKeys.Where(keys => keys.Application.InstanceId == applicationServiceId);
            }

            var keys = applicationKeys.Select(keys => keys.PropertyKey).Distinct();
            return keys.ToList();
        }
        finally
        {
            _logsLock.ExitReadLock();
        }
    }

    public GetTracesResponse GetTraces(GetTracesRequest context)
    {
        _tracesLock.EnterReadLock();

        try
        {
            var results = _traces.AsEnumerable();
            if (context.ApplicationServiceId != null)
            {
                results = results.Where(t => HasApplication(t, context.ApplicationServiceId));
            }
            if (!string.IsNullOrWhiteSpace(context.FilterText))
            {
                results = results.Where(t => t.FullName.Contains(context.FilterText, StringComparison.OrdinalIgnoreCase));
            }

            // Traces can be modified as new spans are added. Copy traces before returning results to avoid concurrency issues.
            var copyFunc = static (OtlpTrace t) => OtlpTrace.Clone(t);

            var pagedResults = OtlpHelpers.GetItems(results, context.StartIndex, context.Count, copyFunc);
            var maxDuration = pagedResults.TotalItemCount > 0 ? results.Max(r => r.Duration) : default;

            return new GetTracesResponse
            {
                PagedResult = pagedResults,
                MaxDuration = maxDuration
            };
        }
        finally
        {
            _tracesLock.ExitReadLock();
        }
    }

    public OtlpTrace? GetTrace(string traceId)
    {
        _tracesLock.EnterReadLock();

        try
        {
            var results = _traces.Where(t => t.TraceId.StartsWith(traceId, StringComparison.Ordinal));
            var trace = results.SingleOrDefault();
            return trace is not null ? OtlpTrace.Clone(trace) : null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Multiple traces found with trace id '{traceId}'.", ex);
        }
        finally
        {
            _tracesLock.ExitReadLock();
        }
    }

    private static bool HasApplication(OtlpTrace t, string applicationServiceId)
    {
        foreach (var span in t.Spans)
        {
            if (span.Source.InstanceId == applicationServiceId)
            {
                return true;
            }
        }
        return false;
    }

    internal void AddMetrics(AddContext context, RepeatedField<ResourceMetrics> resourceMetrics)
    {
        foreach (var rm in resourceMetrics)
        {
            OtlpApplication application;
            try
            {
                application = GetOrAddApplication(rm.Resource);
            }
            catch (Exception ex)
            {
                context.FailureCount += rm.ScopeMetrics.Sum(s => s.Metrics.Count);
                _logger.LogInformation(ex, "Error adding application.");
                continue;
            }

            application.AddMetrics(context, rm.ScopeMetrics);
        }

        RaiseSubscriptionChanged(_metricsSubscriptions);
    }

    public void AddTraces(AddContext context, RepeatedField<ResourceSpans> resourceSpans)
    {
        foreach (var rs in resourceSpans)
        {
            OtlpApplication application;
            try
            {
                application = GetOrAddApplication(rs.Resource);
            }
            catch (Exception ex)
            {
                context.FailureCount += rs.ScopeSpans.Sum(s => s.Spans.Count);
                _logger.LogInformation(ex, "Error adding application.");
                continue;
            }

            AddTracesCore(context, application, rs.ScopeSpans);
        }

        RaiseSubscriptionChanged(_tracesSubscriptions);
    }

    private static OtlpSpanStatusCode ConvertStatus(Status? status)
    {
        return status?.Code switch
        {
            Status.Types.StatusCode.Ok => OtlpSpanStatusCode.Ok,
            Status.Types.StatusCode.Error => OtlpSpanStatusCode.Error,
            Status.Types.StatusCode.Unset => OtlpSpanStatusCode.Unset,
            _ => OtlpSpanStatusCode.Unset
        };
    }

    private static OtlpSpanKind ConvertSpanKind(SpanKind? kind)
    {
        return kind switch
        {
            // Unspecified to Internal is intentional.
            // "Implementations MAY assume SpanKind to be INTERNAL when receiving UNSPECIFIED."
            SpanKind.Unspecified => OtlpSpanKind.Internal,
            SpanKind.Client => OtlpSpanKind.Client,
            SpanKind.Server => OtlpSpanKind.Server,
            SpanKind.Producer => OtlpSpanKind.Producer,
            SpanKind.Consumer => OtlpSpanKind.Consumer,
            _ => OtlpSpanKind.Unspecified
        };
    }

    internal void AddTracesCore(AddContext context, OtlpApplication application, RepeatedField<ScopeSpans> scopeSpans)
    {
        _tracesLock.EnterWriteLock();

        try
        {
            foreach (var scopeSpan in scopeSpans)
            {
                OtlpTraceScope? traceScope;
                try
                {
                    if (!_traceScopes.TryGetValue(scopeSpan.Scope.Name, out traceScope))
                    {
                        traceScope = new OtlpTraceScope(scopeSpan.Scope);
                        _traceScopes.Add(scopeSpan.Scope.Name, traceScope);
                    }
                }
                catch (Exception ex)
                {
                    context.FailureCount += scopeSpan.Spans.Count;
                    _logger.LogInformation(ex, "Error adding scope.");
                    continue;
                }

                OtlpTrace? lastTrace = null;

                foreach (var span in scopeSpan.Spans)
                {
                    try
                    {
                        OtlpTrace? trace;
                        bool newTrace = false;

                        // Fast path to check if the span is in the same trace as the last span.
                        if (lastTrace != null && span.TraceId.Span.SequenceEqual(lastTrace.Key.Span))
                        {
                            trace = lastTrace;
                        }
                        else if (!_traces.TryGetValue(span.TraceId.Memory, out trace))
                        {
                            trace = new OtlpTrace(span.TraceId.Memory, traceScope);
                            newTrace = true;
                        }

                        var newSpan = CreateSpan(application, span, trace);
                        trace.AddSpan(newSpan);

                        // Traces are sorted by the start time of the first span.
                        // We need to ensure traces are in the correct order if we're:
                        // 1. Adding a new trace.
                        // 2. The first span of the trace has changed.
                        if (newTrace)
                        {
                            var added = false;
                            for (var i = _traces.Count - 1; i >= 0; i--)
                            {
                                var currentTrace = _traces[i];
                                if (trace.FirstSpan.StartTime > currentTrace.FirstSpan.StartTime)
                                {
                                    _traces.Insert(i + 1, trace);
                                    added = true;
                                    break;
                                }
                            }
                            if (!added)
                            {
                                _traces.Insert(0, trace);
                            }
                        }
                        else
                        {
                            if (trace.FirstSpan == newSpan)
                            {
                                var moved = false;
                                var index = _traces.IndexOf(trace);

                                for (var i = index - 1; i >= 0; i--)
                                {
                                    var currentTrace = _traces[i];
                                    if (trace.FirstSpan.StartTime > currentTrace.FirstSpan.StartTime)
                                    {
                                        var insertPosition = i + 1;
                                        if (index != insertPosition)
                                        {
                                            _traces.RemoveAt(index);
                                            _traces.Insert(insertPosition, trace);
                                        }
                                        moved = true;
                                        break;
                                    }
                                }
                                if (!moved)
                                {
                                    if (index != 0)
                                    {
                                        _traces.RemoveAt(index);
                                        _traces.Insert(0, trace);
                                    }
                                }
                            }
                        }

                        lastTrace = trace;
                    }
                    catch (Exception ex)
                    {
                        context.FailureCount++;
                        _logger.LogInformation(ex, "Error adding span.");
                    }
                }

            }
        }
        finally
        {
            _tracesLock.ExitWriteLock();
        }
    }

    private static OtlpSpan CreateSpan(OtlpApplication application, Span span, OtlpTrace trace)
    {
        var id = span.SpanId?.ToHexString();
        if (id is null)
        {
            throw new ArgumentException("Span has no SpanId");
        }

        var events = new List<OtlpSpanEvent>();
        foreach (var e in span.Events)
        {
            events.Add(new OtlpSpanEvent()
            {
                Name = e.Name,
                Time = OtlpHelpers.UnixNanoSecondsToDateTime(e.TimeUnixNano),
                Attributes = e.Attributes.ToKeyValuePairs()
            });
        }

        var newSpan = new OtlpSpan(application, trace)
        {
            SpanId = id,
            ParentSpanId = span.ParentSpanId?.ToHexString(),
            Name = span.Name,
            Kind = ConvertSpanKind(span.Kind),
            StartTime = OtlpHelpers.UnixNanoSecondsToDateTime(span.StartTimeUnixNano),
            EndTime = OtlpHelpers.UnixNanoSecondsToDateTime(span.EndTimeUnixNano),
            Status = ConvertStatus(span.Status),
            StatusMessage = span.Status?.Message,
            Attributes = span.Attributes.ToKeyValuePairs(),
            State = span.TraceState,
            Events = events
        };
        return newSpan;
    }

    public List<OtlpInstrument> GetInstrumentsSummary(string applicationServiceId)
    {
        if (!_applications.TryGetValue(applicationServiceId, out var application))
        {
            return new List<OtlpInstrument>();
        }

        return application.GetInstrumentsSummary();
    }

    public OtlpInstrument? GetInstrument(string applicationServiceId, string meterName, string instrumentName)
    {
        if (!_applications.TryGetValue(applicationServiceId, out var application))
        {
            return null;
        }

        return application.GetInstrument(meterName, instrumentName, default, default);
    }
}
