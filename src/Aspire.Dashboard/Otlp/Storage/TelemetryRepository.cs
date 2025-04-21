// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Aspire.Dashboard.Utils;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;
using static OpenTelemetry.Proto.Trace.V1.Span.Types;

namespace Aspire.Dashboard.Otlp.Storage;

public sealed class TelemetryRepository : IDisposable
{
    private readonly PauseManager _pauseManager;
    private readonly IOutgoingPeerResolver[] _outgoingPeerResolvers;
    private readonly ILogger _logger;

    private readonly object _lock = new();
    internal TimeSpan _subscriptionMinExecuteInterval = TimeSpan.FromMilliseconds(100);

    private readonly List<Subscription> _applicationSubscriptions = new();
    private readonly List<Subscription> _logSubscriptions = new();
    private readonly List<Subscription> _metricsSubscriptions = new();
    private readonly List<Subscription> _tracesSubscriptions = new();

    private readonly ConcurrentDictionary<ApplicationKey, OtlpApplication> _applications = new();

    private readonly ReaderWriterLockSlim _logsLock = new();
    private readonly Dictionary<string, OtlpScope> _logScopes = new();
    private readonly CircularBuffer<OtlpLogEntry> _logs;
    private readonly HashSet<(OtlpApplication Application, string PropertyKey)> _logPropertyKeys = new();
    private readonly HashSet<(OtlpApplication Application, string PropertyKey)> _tracePropertyKeys = new();
    private readonly Dictionary<ApplicationKey, int> _applicationUnviewedErrorLogs = new();

    private readonly ReaderWriterLockSlim _tracesLock = new();
    private readonly Dictionary<string, OtlpScope> _traceScopes = new();
    private readonly CircularBuffer<OtlpTrace> _traces;
    private readonly List<OtlpSpanLink> _spanLinks = new();
    private readonly List<IDisposable> _peerResolverSubscriptions = new();
    internal readonly OtlpContext _otlpContext;

    public bool HasDisplayedMaxLogLimitMessage { get; set; }
    public Message? MaxLogLimitMessage { get; set; }

    public bool HasDisplayedMaxTraceLimitMessage { get; set; }
    public Message? MaxTraceLimitMessage { get; set; }

    // For testing.
    internal List<OtlpSpanLink> SpanLinks => _spanLinks;
    internal List<Subscription> TracesSubscriptions => _tracesSubscriptions;

    public TelemetryRepository(ILoggerFactory loggerFactory, IOptions<DashboardOptions> dashboardOptions, PauseManager pauseManager, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
    {
        _logger = loggerFactory.CreateLogger(typeof(TelemetryRepository));
        _otlpContext = new OtlpContext
        {
            Logger = _logger,
            Options = dashboardOptions.Value.TelemetryLimits
        };
        _pauseManager = pauseManager;
        _outgoingPeerResolvers = outgoingPeerResolvers.ToArray();
        _logs = new(_otlpContext.Options.MaxLogCount);
        _traces = new(_otlpContext.Options.MaxTraceCount);
        _traces.ItemRemovedForCapacity += TracesItemRemovedForCapacity;

        foreach (var outgoingPeerResolver in _outgoingPeerResolvers)
        {
            _peerResolverSubscriptions.Add(outgoingPeerResolver.OnPeerChanges(OnPeerChanged));
        }
    }

    private void TracesItemRemovedForCapacity(OtlpTrace trace)
    {
        // Remove links from central collection when the span is removed.
        foreach (var span in trace.Spans)
        {
            foreach (var link in span.Links)
            {
                _spanLinks.Remove(link);
            }
        }
    }

    public List<OtlpApplication> GetApplications(bool includeUninstrumentedPeers = false)
    {
        return GetApplicationsCore(includeUninstrumentedPeers, name: null);
    }

    public List<OtlpApplication> GetApplicationsByName(string name, bool includeUninstrumentedPeers = false)
    {
        return GetApplicationsCore(includeUninstrumentedPeers, name);
    }

    private List<OtlpApplication> GetApplicationsCore(bool includeUninstrumentedPeers, string? name)
    {
        IEnumerable<OtlpApplication> results = _applications.Values;
        if (!includeUninstrumentedPeers)
        {
            results = results.Where(a => !a.UninstrumentedPeer);
        }
        if (name != null)
        {
            results = results.Where(a => string.Equals(a.ApplicationKey.Name, name, StringComparisons.ResourceName));
        }

        var applications = results.OrderBy(a => a.ApplicationKey).ToList();
        return applications;
    }

    public OtlpApplication? GetApplicationByCompositeName(string compositeName)
    {
        foreach (var kvp in _applications)
        {
            if (kvp.Key.EqualsCompositeName(compositeName))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    public OtlpApplication? GetApplication(ApplicationKey key)
    {
        if (key.InstanceId == null)
        {
            throw new InvalidOperationException($"{nameof(ApplicationKey)} must have an instance ID.");
        }

        _applications.TryGetValue(key, out var application);
        return application;
    }

    public List<OtlpApplication> GetApplications(ApplicationKey key, bool includeUninstrumentedPeers = false)
    {
        if (key.InstanceId == null)
        {
            return GetApplicationsByName(key.Name, includeUninstrumentedPeers: includeUninstrumentedPeers);
        }

        var app = GetApplication(key);
        if (app == null || (app.UninstrumentedPeer && !includeUninstrumentedPeers))
        {
            return [];
        }

        return [app];
    }

    public Dictionary<ApplicationKey, int> GetApplicationUnviewedErrorLogsCount()
    {
        _logsLock.EnterReadLock();

        try
        {
            return _applicationUnviewedErrorLogs.ToDictionary();
        }
        finally
        {
            _logsLock.ExitReadLock();
        }
    }

    internal void MarkViewedErrorLogs(ApplicationKey? key)
    {
        _logsLock.EnterWriteLock();

        try
        {
            if (key == null)
            {
                // Mark all logs as viewed.
                if (_applicationUnviewedErrorLogs.Count > 0)
                {
                    _applicationUnviewedErrorLogs.Clear();
                    RaiseSubscriptionChanged(_logSubscriptions);
                }
                return;
            }
            var applications = GetApplications(key.Value);
            foreach (var application in applications)
            {
                // Mark one application logs as viewed.
                if (_applicationUnviewedErrorLogs.Remove(application.ApplicationKey))
                {
                    RaiseSubscriptionChanged(_logSubscriptions);
                }
            }
        }
        finally
        {
            _logsLock.ExitWriteLock();
        }
    }

    private OtlpApplicationView GetOrAddApplicationView(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var key = resource.GetApplicationKey();

        var (application, isNew) = GetOrAddApplication(key, uninstrumentedPeer: false);
        if (isNew)
        {
            RaiseSubscriptionChanged(_applicationSubscriptions);
        }

        return application.GetView(resource.Attributes);
    }

    private (OtlpApplication Application, bool IsNew) GetOrAddApplication(ApplicationKey key, bool uninstrumentedPeer)
    {
        // Fast path.
        if (_applications.TryGetValue(key, out var application))
        {
            application.SetUninstrumentedPeer(uninstrumentedPeer);
            return (Application: application, IsNew: false);
        }

        // Slower get or add path.
        // This GetOrAdd allocates a closure, so we avoid it if possible.
        var newApplication = false;
        application = _applications.GetOrAdd(key, _ =>
        {
            newApplication = true;
            return new OtlpApplication(key.Name, key.InstanceId!, uninstrumentedPeer, _otlpContext);
        });
        if (!newApplication)
        {
            application.SetUninstrumentedPeer(uninstrumentedPeer);
        }
        return (Application: application, IsNew: newApplication);
    }

    public Subscription OnNewApplications(Func<Task> callback)
    {
        return AddSubscription(nameof(OnNewApplications), null, SubscriptionType.Read, callback, _applicationSubscriptions);
    }

    public Subscription OnNewLogs(ApplicationKey? applicationKey, SubscriptionType subscriptionType, Func<Task> callback)
    {
        return AddSubscription(nameof(OnNewLogs), applicationKey, subscriptionType, callback, _logSubscriptions);
    }

    public Subscription OnNewMetrics(ApplicationKey? applicationKey, SubscriptionType subscriptionType, Func<Task> callback)
    {
        return AddSubscription(nameof(OnNewMetrics), applicationKey, subscriptionType, callback, _metricsSubscriptions);
    }

    public Subscription OnNewTraces(ApplicationKey? applicationKey, SubscriptionType subscriptionType, Func<Task> callback)
    {
        return AddSubscription(nameof(OnNewTraces), applicationKey, subscriptionType, callback, _tracesSubscriptions);
    }

    private Subscription AddSubscription(string name, ApplicationKey? applicationKey, SubscriptionType subscriptionType, Func<Task> callback, List<Subscription> subscriptions)
    {
        Subscription? subscription = null;
        subscription = new Subscription(name, applicationKey, subscriptionType, callback, () =>
        {
            lock (_lock)
            {
                subscriptions.Remove(subscription!);
            }
        }, ExecutionContext.Capture(), this);

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
                subscription.Execute();
            }
        }
    }

    public void AddLogs(AddContext context, RepeatedField<ResourceLogs> resourceLogs)
    {
        if (_pauseManager.AreStructuredLogsPaused(out _))
        {
            _logger.LogTrace("{Count} incoming structured log(s) ignored because of an active pause.", resourceLogs.Count);
            return;
        }

        foreach (var rl in resourceLogs)
        {
            OtlpApplicationView applicationView;
            try
            {
                applicationView = GetOrAddApplicationView(rl.Resource);
            }
            catch (Exception ex)
            {
                context.FailureCount += rl.ScopeLogs.Count;
                _otlpContext.Logger.LogInformation(ex, "Error adding application.");
                continue;
            }

            AddLogsCore(context, applicationView, rl.ScopeLogs);
        }

        RaiseSubscriptionChanged(_logSubscriptions);
    }

    private bool TryAddScope(Dictionary<string, OtlpScope> scopes, InstrumentationScope? scope, [NotNullWhen(true)] out OtlpScope? s)
    {
        try
        {
            // The instrumentation scope information for the spans in this message.
            // Semantically when InstrumentationScope isn't set, it is equivalent with
            // an empty instrumentation scope name (unknown).
            var name = scope?.Name ?? string.Empty;
            ref var scopeRef = ref CollectionsMarshal.GetValueRefOrAddDefault(scopes, name, out _);
            // Adds to dictionary if not present.
            scopeRef ??= (scope != null) ? new OtlpScope(scope, _otlpContext) : OtlpScope.Empty;
            s = scopeRef;
            return true;
        }
        catch (Exception ex)
        {
            _otlpContext.Logger.LogInformation(ex, "Error adding scope.");
            s = null;
            return false;
        }
    }

    public void AddLogsCore(AddContext context, OtlpApplicationView applicationView, RepeatedField<ScopeLogs> scopeLogs)
    {
        _logsLock.EnterWriteLock();

        try
        {
            foreach (var sl in scopeLogs)
            {
                if (!TryAddScope(_logScopes, sl.Scope, out var scope))
                {
                    context.FailureCount += sl.LogRecords.Count;
                    continue;
                }

                foreach (var record in sl.LogRecords)
                {
                    try
                    {
                        var logEntry = new OtlpLogEntry(record, applicationView, scope, _otlpContext);

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

                        // For log entries error and above, increment the unviewed count if there are no read log subscriptions for the application.
                        // We don't increment the count if there are active read subscriptions because the count will be quickly decremented when the subscription callback is run.
                        // Notifying the user there are errors and then immediately clearing the notification is confusing.
                        if (logEntry.Severity >= LogLevel.Error)
                        {
                            if (!_logSubscriptions.Any(s => s.SubscriptionType == SubscriptionType.Read && (s.ApplicationKey == applicationView.ApplicationKey || s.ApplicationKey == null)))
                            {
                                ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(_applicationUnviewedErrorLogs, applicationView.ApplicationKey, out _);
                                // Adds to dictionary if not present.
                                count++;
                            }
                        }

                        foreach (var kvp in logEntry.Attributes)
                        {
                            _logPropertyKeys.Add((applicationView.Application, kvp.Key));
                        }
                    }
                    catch (Exception ex)
                    {
                        context.FailureCount++;
                        _otlpContext.Logger.LogInformation(ex, "Error adding log entry.");
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
        List<OtlpApplication>? applications = null;
        if (context.ApplicationKey is { } key)
        {
            applications = GetApplications(key);

            if (applications.Count == 0)
            {
                return PagedResult<OtlpLogEntry>.Empty;
            }
        }

        _logsLock.EnterReadLock();

        try
        {
            var results = _logs.AsEnumerable();
            if (applications?.Count > 0)
            {
                results = results.Where(l => MatchApplications(l.ApplicationView.ApplicationKey, applications));
            }

            foreach (var filter in context.Filters.GetEnabledFilters())
            {
                results = filter.Apply(results);
            }

            return OtlpHelpers.GetItems(results, context.StartIndex, context.Count, _logs.IsFull);
        }
        finally
        {
            _logsLock.ExitReadLock();
        }
    }

    public List<string> GetLogPropertyKeys(ApplicationKey? applicationKey)
    {
        List<OtlpApplication>? applications = null;
        if (applicationKey != null)
        {
            applications = GetApplications(applicationKey.Value);
        }

        _logsLock.EnterReadLock();

        try
        {
            var applicationKeys = _logPropertyKeys.AsEnumerable();
            if (applications?.Count > 0)
            {
                applicationKeys = applicationKeys.Where(keys => MatchApplications(keys.Application.ApplicationKey, applications));
            }

            var keys = applicationKeys.Select(keys => keys.PropertyKey).Distinct();
            return keys.OrderBy(k => k).ToList();
        }
        finally
        {
            _logsLock.ExitReadLock();
        }
    }

    public List<string> GetTracePropertyKeys(ApplicationKey? applicationKey)
    {
        List<OtlpApplication>? applications = null;
        if (applicationKey != null)
        {
            applications = GetApplications(applicationKey.Value, includeUninstrumentedPeers: true);
        }

        _tracesLock.EnterReadLock();

        try
        {
            var applicationKeys = _tracePropertyKeys.AsEnumerable();
            if (applications?.Count > 0)
            {
                applicationKeys = applicationKeys.Where(keys => MatchApplications(keys.Application.ApplicationKey, applications));
            }

            var keys = applicationKeys.Select(keys => keys.PropertyKey).Distinct();
            return keys.OrderBy(k => k).ToList();
        }
        finally
        {
            _tracesLock.ExitReadLock();
        }
    }

    public GetTracesResponse GetTraces(GetTracesRequest context)
    {
        List<OtlpApplication>? applications = null;
        if (context.ApplicationKey is { } key)
        {
            applications = GetApplications(key, includeUninstrumentedPeers: true);

            if (applications.Count == 0)
            {
                return new GetTracesResponse
                {
                    PagedResult = PagedResult<OtlpTrace>.Empty,
                    MaxDuration = TimeSpan.Zero
                };
            }
        }

        _tracesLock.EnterReadLock();

        try
        {
            var results = _traces.AsEnumerable();
            if (applications?.Count > 0)
            {
                results = results.Where(t =>
                {
                    return MatchApplications(t, applications);
                });
            }
            if (!string.IsNullOrWhiteSpace(context.FilterText))
            {
                results = results.Where(t => t.FullName.Contains(context.FilterText, StringComparison.OrdinalIgnoreCase));
            }

            var filters = context.Filters.GetEnabledFilters().ToList();

            if (filters.Count > 0)
            {
                results = results.Where(t =>
                {
                    // A trace matches when one of its span matches all filters.
                    foreach (var span in t.Spans)
                    {
                        var match = true;
                        foreach (var filter in filters)
                        {
                            if (!filter.Apply(span))
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match)
                        {
                            return true;
                        }
                    }

                    return false;
                });
            }

            // Traces can be modified as new spans are added. Copy traces before returning results to avoid concurrency issues.
            var copyFunc = static (OtlpTrace t) => OtlpTrace.Clone(t);

            var pagedResults = OtlpHelpers.GetItems(results, context.StartIndex, context.Count, _traces.IsFull, copyFunc);
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

    private static bool MatchApplications(ApplicationKey applicationKey, List<OtlpApplication> applications)
    {
        foreach (var application in applications)
        {
            if (applicationKey == application.ApplicationKey)
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchApplications(OtlpTrace t, List<OtlpApplication> applications)
    {
        for (var i = 0; i < applications.Count; i++)
        {
            var applicationKey = applications[i].ApplicationKey;

            // Spans collection type returns a struct enumerator so it's ok to foreach inside another loop.
            foreach (var span in t.Spans)
            {
                if (span.Source.ApplicationKey == applicationKey || span.UninstrumentedPeer?.ApplicationKey == applicationKey)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void ClearAllSignals()
    {
        ClearTraces(null);
        ClearStructuredLogs(null);
        ClearMetrics(null);
    }

    public void ClearTraces(ApplicationKey? applicationKey = null)
    {
        List<OtlpApplication>? applications = null;
        if (applicationKey.HasValue)
        {
            applications = GetApplications(applicationKey.Value, includeUninstrumentedPeers: true);
        }

        _tracesLock.EnterWriteLock();
        try
        {
            if (applications is null || applications.Count == 0)
            {
                // Nothing selected, clear everything.
                _traces.Clear();
            }
            else
            {
                for (var i = _traces.Count - 1; i >= 0; i--)
                {
                    // Remove trace if any span matches one of the applications. This matches filter behavior.
                    if (MatchApplications(_traces[i], applications))
                    {
                        _traces.RemoveAt(i);
                        continue;
                    }
                }
            }
        }
        finally
        {
            _tracesLock.ExitWriteLock();
        }

        RaiseSubscriptionChanged(_tracesSubscriptions);
    }

    public void ClearStructuredLogs(ApplicationKey? applicationKey = null)
    {
        List<OtlpApplication>? applications = null;
        if (applicationKey.HasValue)
        {
            applications = GetApplications(applicationKey.Value);
        }

        _logsLock.EnterWriteLock();

        try
        {
            if (applications is null || applications.Count == 0)
            {
                // Nothing selected, clear everything.
                _logs.Clear();
            }
            else
            {
                for (var i = _logs.Count - 1; i >= 0; i--)
                {
                    if (MatchApplications(_logs[i].ApplicationView.ApplicationKey, applications))
                    {
                        _logs.RemoveAt(i);
                        continue;
                    }
                }
            }
        }
        finally
        {
            _logsLock.ExitWriteLock();
        }

        RaiseSubscriptionChanged(_logSubscriptions);
    }

    public void ClearMetrics(ApplicationKey? applicationKey = null)
    {
        List<OtlpApplication> applications;
        if (applicationKey.HasValue)
        {
            applications = GetApplications(applicationKey.Value);
        }
        else
        {
            applications = _applications.Values.ToList();
        }

        foreach (var app in applications)
        {
            app.ClearMetrics();
        }

        RaiseSubscriptionChanged(_metricsSubscriptions);
    }

    public Dictionary<string, int> GetTraceFieldValues(string attributeName)
    {
        _tracesLock.EnterReadLock();

        var attributesValues = new Dictionary<string, int>(StringComparers.OtlpAttribute);

        try
        {
            foreach (var trace in _traces)
            {
                foreach (var span in trace.Spans)
                {
                    var values = OtlpSpan.GetFieldValue(span, attributeName);
                    if (values.Value1 != null)
                    {
                        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(attributesValues, values.Value1, out _);
                        // Adds to dictionary if not present.
                        count++;
                    }
                    if (values.Value2 != null)
                    {
                        ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(attributesValues, values.Value2, out _);
                        // Adds to dictionary if not present.
                        count++;
                    }
                }
            }
        }
        finally
        {
            _tracesLock.ExitReadLock();
        }

        return attributesValues;
    }

    public Dictionary<string, int> GetLogsFieldValues(string attributeName)
    {
        _logsLock.EnterReadLock();

        var attributesValues = new Dictionary<string, int>(StringComparers.OtlpAttribute);

        try
        {
            foreach (var log in _logs)
            {
                var value = OtlpLogEntry.GetFieldValue(log, attributeName);
                if (value != null)
                {
                    ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(attributesValues, value, out _);
                    // Adds to dictionary if not present.
                    count++;
                }
            }
        }
        finally
        {
            _logsLock.ExitReadLock();
        }

        return attributesValues;
    }

    public OtlpTrace? GetTrace(string traceId)
    {
        _tracesLock.EnterReadLock();

        try
        {
            return GetTraceUnsynchronized(traceId);
        }
        finally
        {
            _tracesLock.ExitReadLock();
        }
    }

    private OtlpTrace? GetTraceUnsynchronized(string traceId)
    {
        Debug.Assert(_tracesLock.IsReadLockHeld || _tracesLock.IsWriteLockHeld, $"Must get lock before calling {nameof(GetTraceUnsynchronized)}.");

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
    }

    private OtlpSpan? GetSpanUnsynchronized(string traceId, string spanId)
    {
        Debug.Assert(_tracesLock.IsReadLockHeld || _tracesLock.IsWriteLockHeld, $"Must get lock before calling {nameof(GetSpanUnsynchronized)}.");

        var trace = GetTraceUnsynchronized(traceId);
        if (trace != null)
        {
            foreach (var span in trace.Spans)
            {
                if (span.SpanId == spanId)
                {
                    return span;
                }
            }
        }

        return null;
    }

    public OtlpSpan? GetSpan(string traceId, string spanId)
    {
        _tracesLock.EnterReadLock();

        try
        {
            return GetSpanUnsynchronized(traceId, spanId);
        }
        finally
        {
            _tracesLock.ExitReadLock();
        }
    }

    public void AddMetrics(AddContext context, RepeatedField<ResourceMetrics> resourceMetrics)
    {
        if (_pauseManager.AreMetricsPaused(out _))
        {
            _logger.LogTrace("{Count} incoming metric(s) ignored because of an active pause.", resourceMetrics.Count);
            return;
        }

        foreach (var rm in resourceMetrics)
        {
            OtlpApplicationView applicationView;
            try
            {
                applicationView = GetOrAddApplicationView(rm.Resource);
            }
            catch (Exception ex)
            {
                context.FailureCount += rm.ScopeMetrics.Sum(s => s.Metrics.Count);
                _otlpContext.Logger.LogInformation(ex, "Error adding application.");
                continue;
            }

            applicationView.Application.AddMetrics(context, rm.ScopeMetrics);
        }

        RaiseSubscriptionChanged(_metricsSubscriptions);
    }

    public void AddTraces(AddContext context, RepeatedField<ResourceSpans> resourceSpans)
    {
        if (_pauseManager.AreTracesPaused(out _))
        {
            _logger.LogTrace("{Count} incoming trace(s) ignored because of an active pause.", resourceSpans.Count);
            return;
        }

        foreach (var rs in resourceSpans)
        {
            OtlpApplicationView applicationView;
            try
            {
                applicationView = GetOrAddApplicationView(rs.Resource);
            }
            catch (Exception ex)
            {
                context.FailureCount += rs.ScopeSpans.Sum(s => s.Spans.Count);
                _otlpContext.Logger.LogInformation(ex, "Error adding application.");
                continue;
            }

            AddTracesCore(context, applicationView, rs.ScopeSpans);
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

    internal static OtlpSpanKind ConvertSpanKind(SpanKind? kind)
    {
        return kind switch
        {
            // Unspecified to Internal is intentional.
            // "Implementations MAY assume SpanKind to be INTERNAL when receiving UNSPECIFIED."
            SpanKind.Unspecified => OtlpSpanKind.Internal,
            SpanKind.Internal => OtlpSpanKind.Internal,
            SpanKind.Client => OtlpSpanKind.Client,
            SpanKind.Server => OtlpSpanKind.Server,
            SpanKind.Producer => OtlpSpanKind.Producer,
            SpanKind.Consumer => OtlpSpanKind.Consumer,
            _ => OtlpSpanKind.Unspecified
        };
    }

    internal void AddTracesCore(AddContext context, OtlpApplicationView applicationView, RepeatedField<ScopeSpans> scopeSpans)
    {
        _tracesLock.EnterWriteLock();

        try
        {
            foreach (var scopeSpan in scopeSpans)
            {
                if (!TryAddScope(_traceScopes, scopeSpan.Scope, out var scope))
                {
                    context.FailureCount += scopeSpan.Spans.Count;
                    continue;
                }

                var updatedTraces = new Dictionary<ReadOnlyMemory<byte>, OtlpTrace>();

                foreach (var span in scopeSpan.Spans)
                {
                    try
                    {
                        OtlpTrace? trace;
                        var newTrace = false;

                        // Fast path to check if the span is in a trace that's been updated this add call.
                        if (!updatedTraces.TryGetValue(span.TraceId.Memory, out trace))
                        {
                            if (!TryGetTraceById(_traces, span.TraceId.Memory, out trace))
                            {
                                trace = new OtlpTrace(span.TraceId.Memory);
                                newTrace = true;
                            }
                        }

                        var newSpan = CreateSpan(applicationView, span, trace, scope, _otlpContext);
                        trace.AddSpan(newSpan);

                        // The new span might be linked to by an existing span.
                        // Check current links to see if a backlink should be created.
                        foreach (var existingLink in _spanLinks)
                        {
                            if (existingLink.SpanId == newSpan.SpanId && existingLink.TraceId == newSpan.TraceId)
                            {
                                newSpan.BackLinks.Add(existingLink);
                            }
                        }

                        // Add links to central collection. Add backlinks to existing spans.
                        foreach (var link in newSpan.Links)
                        {
                            _spanLinks.Add(link);

                            var linkedSpan = GetSpanUnsynchronized(link.TraceId, link.SpanId);
                            linkedSpan?.BackLinks.Add(link);
                        }

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

                        foreach (var kvp in newSpan.Attributes)
                        {
                            _tracePropertyKeys.Add((applicationView.Application, kvp.Key));
                        }

                        // Newly added or updated trace should always been in the collection.
                        Debug.Assert(_traces.Contains(trace), "Trace not found in traces collection.");

                        updatedTraces[trace.Key] = trace;
                    }
                    catch (Exception ex)
                    {
                        context.FailureCount++;
                        _otlpContext.Logger.LogInformation(ex, "Error adding span.");
                    }

                    AssertTraceOrder();
                    AssertSpanLinks();
                }

                // After spans are updated, loop through traces and their spans and update uninstrumented peer values.
                // These can change
                foreach (var (_, updatedTrace) in updatedTraces)
                {
                    CalculateTraceUninstrumentedPeers(updatedTrace);
                }
            }
        }
        finally
        {
            _tracesLock.ExitWriteLock();
        }

        static bool TryGetTraceById(CircularBuffer<OtlpTrace> traces, ReadOnlyMemory<byte> traceId, [NotNullWhen(true)] out OtlpTrace? trace)
        {
            var s = traceId.Span;
            for (var i = traces.Count - 1; i >= 0; i--)
            {
                if (traces[i].Key.Span.SequenceEqual(s))
                {
                    trace = traces[i];
                    return true;
                }
            }

            trace = null;
            return false;
        }
    }

    private void CalculateTraceUninstrumentedPeers(OtlpTrace trace)
    {
        foreach (var span in trace.Spans)
        {
            // A span may indicate a call to another service but the service isn't instrumented.
            var hasPeerService = OtlpHelpers.GetPeerAddress(span.Attributes) != null;
            var hasUninstrumentedPeer = hasPeerService && span.Kind is OtlpSpanKind.Client or OtlpSpanKind.Producer && !span.GetChildSpans().Any();
            var uninstrumentedPeer = hasUninstrumentedPeer ? ResolveUninstrumentedPeerResource(span, _outgoingPeerResolvers) : null;

            if (uninstrumentedPeer != null)
            {
                if (span.UninstrumentedPeer?.ApplicationKey.EqualsCompositeName(uninstrumentedPeer.Name) ?? false)
                {
                    // Already the correct value. No changes needed.
                    continue;
                }

                var appKey = ApplicationKey.Create(uninstrumentedPeer.Name);
                var (app, _) = GetOrAddApplication(appKey, uninstrumentedPeer: true);
                span.UninstrumentedPeer = app;
            }
            else
            {
                span.UninstrumentedPeer = null;
            }
        }
    }

    private static ResourceViewModel? ResolveUninstrumentedPeerResource(OtlpSpan span, IEnumerable<IOutgoingPeerResolver> outgoingPeerResolvers)
    {
        // Attempt to resolve uninstrumented peer to a friendly name from the span.
        foreach (var resolver in outgoingPeerResolvers)
        {
            if (resolver.TryResolvePeer(span.Attributes, out _, out var matchedResourced))
            {
                return matchedResourced;
            }
        }

        return null;
    }

    [Conditional("DEBUG")]
    private void AssertTraceOrder()
    {
        DateTime current = default;
        for (var i = 0; i < _traces.Count; i++)
        {
            var trace = _traces[i];
            if (trace.FirstSpan.StartTime < current)
            {
                throw new InvalidOperationException($"Traces not in order at index {i}.");
            }

            current = trace.FirstSpan.StartTime;
        }
    }

    [Conditional("DEBUG")]
    private void AssertSpanLinks()
    {
        // Create a local copy of span links.
        var currentSpanLinks = _spanLinks.ToList();

        // Remove span links that match span links on spans.
        // Throw an error if an expected span link doesn't exist.
        foreach (var trace in _traces)
        {
            foreach (var span in trace.Spans)
            {
                foreach (var link in span.Links)
                {
                    if (!currentSpanLinks.Remove(link))
                    {
                        throw new InvalidOperationException($"Couldn't find expected link from span {span.SpanId} to span {link.SpanId}.");
                    }
                }
            }
        }

        // Throw error if there are orphaned span links.
        if (currentSpanLinks.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CultureInfo.InvariantCulture, $"There are {currentSpanLinks.Count} orphaned span links.");
            foreach (var link in currentSpanLinks)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"\tSource span ID: {link.SourceSpanId}, Target span ID: {link.SpanId}");
            }

            throw new InvalidOperationException(sb.ToString());
        }
    }

    private static OtlpSpan CreateSpan(OtlpApplicationView applicationView, Span span, OtlpTrace trace, OtlpScope scope, OtlpContext context)
    {
        var id = span.SpanId?.ToHexString();
        if (id is null)
        {
            throw new ArgumentException("Span has no SpanId");
        }

        var events = new List<OtlpSpanEvent>();

        var links = new List<OtlpSpanLink>();
        foreach (var e in span.Links)
        {
            links.Add(new OtlpSpanLink
            {
                SourceSpanId = id,
                SourceTraceId = trace.TraceId,
                TraceState = e.TraceState,
                SpanId = e.SpanId.ToHexString(),
                TraceId = e.TraceId.ToHexString(),
                Attributes = e.Attributes.ToKeyValuePairs(context)
            });
        }

        var newSpan = new OtlpSpan(applicationView, trace, scope)
        {
            SpanId = id,
            ParentSpanId = span.ParentSpanId?.ToHexString(),
            Name = span.Name,
            Kind = ConvertSpanKind(span.Kind),
            StartTime = OtlpHelpers.UnixNanoSecondsToDateTime(span.StartTimeUnixNano),
            EndTime = OtlpHelpers.UnixNanoSecondsToDateTime(span.EndTimeUnixNano),
            Status = ConvertStatus(span.Status),
            StatusMessage = span.Status?.Message,
            Attributes = span.Attributes.ToKeyValuePairs(context),
            State = span.TraceState,
            Events = events,
            Links = links,
            BackLinks = new()
        };

        foreach (var e in span.Events.OrderBy(e => e.TimeUnixNano))
        {
            events.Add(new OtlpSpanEvent(newSpan)
            {
                InternalId = Guid.NewGuid(),
                Name = e.Name,
                Time = OtlpHelpers.UnixNanoSecondsToDateTime(e.TimeUnixNano),
                Attributes = e.Attributes.ToKeyValuePairs(context)
            });

            if (events.Count >= context.Options.MaxSpanEventCount)
            {
                break;
            }
        }
        return newSpan;
    }

    public List<OtlpInstrumentSummary> GetInstrumentsSummaries(ApplicationKey key)
    {
        var applications = GetApplications(key);
        if (applications.Count == 0)
        {
            return new List<OtlpInstrumentSummary>();
        }
        else if (applications.Count == 1)
        {
            return applications[0].GetInstrumentsSummary();
        }
        else
        {
            var allApplicationSummaries = applications
                .SelectMany(a => a.GetInstrumentsSummary())
                .DistinctBy(s => s.GetKey())
                .ToList();

            return allApplicationSummaries;
        }

    }

    public OtlpInstrumentData? GetInstrument(GetInstrumentRequest request)
    {
        var applications = GetApplications(request.ApplicationKey);
        var instruments = applications
            .Select(a => a.GetInstrument(request.MeterName, request.InstrumentName, request.StartTime, request.EndTime))
            .OfType<OtlpInstrument>()
            .ToList();

        if (instruments.Count == 0)
        {
            return null;
        }
        else if (instruments.Count == 1)
        {
            var instrument = instruments[0];
            return new OtlpInstrumentData
            {
                Summary = instrument.Summary,
                KnownAttributeValues = instrument.KnownAttributeValues,
                Dimensions = instrument.Dimensions.Values.ToList(),
                HasOverflow = instrument.HasOverflow
            };
        }
        else
        {
            var allDimensions = new List<DimensionScope>();
            var allKnownAttributes = new Dictionary<string, List<string?>>();
            var hasOverflow = false;

            foreach (var instrument in instruments)
            {
                allDimensions.AddRange(instrument.Dimensions.Values);

                foreach (var knownAttributeValues in instrument.KnownAttributeValues)
                {
                    ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(allKnownAttributes, knownAttributeValues.Key, out _);
                    // Adds to dictionary if not present.
                    if (values != null)
                    {
                        values = values.Union(knownAttributeValues.Value).ToList();
                    }
                    else
                    {
                        values = knownAttributeValues.Value.ToList();
                    }
                }

                hasOverflow = hasOverflow || instrument.HasOverflow;
            }

            return new OtlpInstrumentData
            {
                Summary = instruments[0].Summary,
                Dimensions = allDimensions,
                KnownAttributeValues = allKnownAttributes,
                HasOverflow = hasOverflow
            };
        }
    }

    private Task OnPeerChanged()
    {
        _tracesLock.EnterWriteLock();

        try
        {
            // When peers change then we need to recalculate the uninstrumented peers of spans.
            foreach (var trace in _traces)
            {
                CalculateTraceUninstrumentedPeers(trace);
            }
        }
        finally
        {
            _tracesLock.ExitWriteLock();
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        foreach (var subscription in _peerResolverSubscriptions)
        {
            subscription.Dispose();
        }
    }
}
