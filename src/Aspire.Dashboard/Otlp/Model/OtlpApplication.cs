// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("ApplicationName = {ApplicationName}, InstanceId = {InstanceId}")]
public class OtlpApplication
{
    public const string SERVICE_NAME = "service.name";
    public const string SERVICE_INSTANCE_ID = "service.instance.id";

    public string ApplicationName { get; }
    public string InstanceId { get; }
    public int Suffix { get; }

    private readonly ReaderWriterLockSlim _logsLock = new();
    private readonly List<OtlpLogEntry> _logs = new();
    private readonly HashSet<string> _logPropertyKeys = new();

    private readonly ReaderWriterLockSlim _metricsLock = new();
    private readonly Dictionary<string, OtlpMeter> _meters = new();

    private readonly ReaderWriterLockSlim _traceSpanLock = new();
    private readonly Dictionary<string, OtlpTraceScope> _traceScopes = new();
    private readonly ILogger _logger;

    public KeyValuePair<string, string>[] Properties { get; }

    public OtlpApplication(Resource resource, IReadOnlyDictionary<string, OtlpApplication> applications, ILogger logger)
    {
        var properties = new List<KeyValuePair<string, string>>();
        foreach (var attribute in resource.Attributes)
        {
            switch (attribute.Key)
            {
                case SERVICE_NAME:
                    ApplicationName = attribute.Value.GetString();
                    break;
                case SERVICE_INSTANCE_ID:
                    InstanceId = attribute.Value.GetString();
                    break;
                default:
                    properties.Add(new KeyValuePair<string, string>(attribute.Key, attribute.Value.GetString()));
                    break;

            }
        }
        Properties = properties.ToArray();
        if (string.IsNullOrEmpty(ApplicationName))
        {
            ApplicationName = "Unknown";
        }
        if (string.IsNullOrEmpty(InstanceId))
        {
            throw new ArgumentException("Resource needs to include a 'service.instance.id'");
        }
        Suffix = applications.Where(a => a.Value.ApplicationName == ApplicationName).Count();
        _logger = logger;
    }

    public string UniqueApplicationName => $"{ApplicationName}-{Suffix}";

    public string ShortApplicationName
    {
        get
        {
            var n = ApplicationName + Suffix.ToString(CultureInfo.InvariantCulture);
            return n.Length <= 10 ? n : $"{ApplicationName.Left(3)}…{ApplicationName.Right(5)}{Suffix}";
        }
    }

    public void AddLogs(AddContext context, RepeatedField<ScopeLogs> scopeLogs)
    {
        _logsLock.EnterReadLock();

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
                        var logEntry = new OtlpLogEntry(record, this);
                        _logs.Add(logEntry);
                        foreach (var kvp in logEntry.Properties)
                        {
                            _logPropertyKeys.Add(kvp.Key);
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
            _logsLock.ExitReadLock();
        }
    }

    public PagedResult<OtlpTraceScope> GetTraces(GetTracesContext context)
    {
        _logsLock.EnterReadLock();

        try
        {
            var results = _traceScopes.Values.AsEnumerable();

            var items = GetItems(results, context.StartIndex, context.Count);
            var count = results.Count();

            return new PagedResult<OtlpTraceScope>
            {
                Items = items,
                TotalItemCount = count
            };
        }
        finally
        {
            _logsLock.ExitReadLock();
        }
    }

    public PagedResult<OtlpLogEntry> GetLogs(GetLogsContext context)
    {
        _logsLock.EnterReadLock();

        try
        {
            var results = _logs.AsEnumerable();
            foreach (var filter in context.Filters)
            {
                results = filter.Apply(results);
            }

            var items = GetItems(results, context.StartIndex, context.Count);
            var count = results.Count();
            
            return new PagedResult<OtlpLogEntry>
            {
                Items = items,
                TotalItemCount = count
            };
        }
        finally
        {
            _logsLock.ExitReadLock();
        }
    }

    private static List<T> GetItems<T>(IEnumerable<T> results, int startIndex, int? count)
    {
        var query = results.Skip(startIndex);
        if (count != null)
        {
            query = query.Take(count.Value);
        }
        return query.ToList();
    }

    public List<string> GetLogPropertyKeys()
    {
        _logsLock.EnterReadLock();

        try
        {
            return _logPropertyKeys.ToList();
        }
        finally
        {
            _logsLock.ExitReadLock();
        }
    }

    public void AddMetrics(AddContext context, RepeatedField<ScopeMetrics> scopeMetrics)
    {
        _metricsLock.EnterReadLock();

        try
        {
            foreach (var sm in scopeMetrics)
            {
                OtlpMeter? meter;

                try
                {
                    if (!_meters.TryGetValue(sm.Scope.Name, out meter))
                    {
                        meter = new OtlpMeter(sm.Scope);
                        _meters.Add(sm.Scope.Name, meter);
                    }
                }
                catch (Exception ex)
                {
                    context.FailureCount += sm.Metrics.Count;
                    _logger.LogInformation(ex, "Error adding meter.");
                    continue;
                }

                foreach (var metric in sm.Metrics)
                {
                    try
                    {
                        if (!meter.Instruments.TryGetValue(metric.Name, out var instrument))
                        {
                            instrument = new OtlpInstrument(metric, meter);
                            meter.Instruments.Add(instrument.Name, instrument);
                        }

                        instrument.AddInstrumentValuesFromGrpc(metric);
                    }
                    catch (Exception ex)
                    {
                        context.FailureCount++;
                        _logger.LogInformation(ex, "Error adding metric.");
                    }
                }
            }
        }
        finally
        {
            _metricsLock.ExitReadLock();
        }
    }

    internal void AddTraces(AddContext context, RepeatedField<ScopeSpans> scopeSpans)
    {
        _traceSpanLock.EnterReadLock();

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
                    }
                }
                catch (Exception ex)
                {
                    context.FailureCount += scopeSpan.Spans.Count;
                    _logger.LogInformation(ex, "Error adding scope.");
                    continue;
                }

                foreach (var span in scopeSpan.Spans)
                {
                    try
                    {
                        traceScope.TraceSpans.Add(new OtlpTraceSpan(span, this, traceScope));
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
            _traceSpanLock.ExitReadLock();
        }
    }
}
