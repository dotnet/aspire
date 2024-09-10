// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("ApplicationName = {ApplicationName}, InstanceId = {InstanceId}")]
public class OtlpApplication
{
    public const string SERVICE_NAME = "service.name";
    public const string SERVICE_INSTANCE_ID = "service.instance.id";
    public const string PROCESS_EXECUTABLE_NAME = "process.executable.name";

    public string ApplicationName { get; }
    public string InstanceId { get; }

    public ApplicationKey ApplicationKey => new ApplicationKey(ApplicationName, InstanceId);

    private readonly ReaderWriterLockSlim _metricsLock = new();
    private readonly Dictionary<string, OtlpMeter> _meters = new();
    private readonly Dictionary<OtlpInstrumentKey, OtlpInstrument> _instruments = new();
    private readonly ConcurrentDictionary<KeyValuePair<string, string>[], OtlpApplicationView> _applicationViews = new(ApplicationViewKeyComparer.Instance);

    private readonly ILogger _logger;
    private readonly TelemetryLimitOptions _options;

    public OtlpApplication(string name, string instanceId, ILogger logger, TelemetryLimitOptions options)
    {
        ApplicationName = name;
        InstanceId = instanceId;

        _logger = logger;
        _options = options;
    }

    public void AddMetrics(AddContext context, RepeatedField<ScopeMetrics> scopeMetrics)
    {
        _metricsLock.EnterWriteLock();

        try
        {
            // Temporary attributes array to use when adding metrics to the instruments.
            KeyValuePair<string, string>[]? tempAttributes = null;

            foreach (var sm in scopeMetrics)
            {
                foreach (var metric in sm.Metrics)
                {
                    try
                    {
                        var instrumentKey = new OtlpInstrumentKey(sm.Scope.Name, metric.Name);
                        if (!_instruments.TryGetValue(instrumentKey, out var instrument))
                        {
                            _instruments.Add(instrumentKey, instrument = new OtlpInstrument
                            {
                                Summary = new OtlpInstrumentSummary
                                {
                                    Name = metric.Name,
                                    Description = metric.Description,
                                    Unit = metric.Unit,
                                    Type = MapMetricType(metric.DataCase),
                                    Parent = GetMeter(sm.Scope)
                                },
                                Options = _options
                            });
                        }

                        instrument.AddMetrics(metric, ref tempAttributes);
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
            _metricsLock.ExitWriteLock();
        }
    }

    private static OtlpInstrumentType MapMetricType(Metric.DataOneofCase data)
    {
        return data switch
        {
            Metric.DataOneofCase.Gauge => OtlpInstrumentType.Gauge,
            Metric.DataOneofCase.Sum => OtlpInstrumentType.Sum,
            Metric.DataOneofCase.Histogram => OtlpInstrumentType.Histogram,
            _ => OtlpInstrumentType.Unsupported
        };
    }

    private OtlpMeter GetMeter(InstrumentationScope scope)
    {
        if (!_meters.TryGetValue(scope.Name, out var meter))
        {
            _meters.Add(scope.Name, meter = new OtlpMeter(scope, _options));
        }
        return meter;
    }

    public OtlpInstrument? GetInstrument(string meterName, string instrumentName, DateTime? valuesStart, DateTime? valuesEnd)
    {
        _metricsLock.EnterReadLock();

        try
        {
            if (!_instruments.TryGetValue(new OtlpInstrumentKey(meterName, instrumentName), out var instrument))
            {
                return null;
            }

            return OtlpInstrument.Clone(instrument, cloneData: true, valuesStart: valuesStart, valuesEnd: valuesEnd);
        }
        finally
        {
            _metricsLock.ExitReadLock();
        }
    }

    public List<OtlpInstrumentSummary> GetInstrumentsSummary()
    {
        _metricsLock.EnterReadLock();

        try
        {
            var instruments = new List<OtlpInstrumentSummary>(_instruments.Count);
            foreach (var instrument in _instruments)
            {
                instruments.Add(instrument.Value.Summary);
            }
            return instruments;
        }
        finally
        {
            _metricsLock.ExitReadLock();
        }
    }

    public static Dictionary<string, List<OtlpApplication>> GetReplicasByApplicationName(IEnumerable<OtlpApplication> allApplications)
    {
        return allApplications
            .GroupBy(application => application.ApplicationName, StringComparers.ResourceName)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
    }

    public static string GetResourceName(OtlpApplicationView app, List<OtlpApplication> allApplications) =>
        GetResourceName(app.Application, allApplications);

    public static string GetResourceName(OtlpApplication app, List<OtlpApplication> allApplications)
    {
        var count = 0;
        foreach (var item in allApplications)
        {
            if (string.Equals(item.ApplicationName, app.ApplicationName, StringComparisons.ResourceName))
            {
                count++;
                if (count >= 2)
                {
                    var instanceId = app.InstanceId;

                    // Convert long GUID into a shorter, more human friendly format.
                    // Before: aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee
                    // After:  aaaaaaaa
                    if (Guid.TryParse(instanceId, out var guid))
                    {
                        Span<char> chars = stackalloc char[32];
                        var result = guid.TryFormat(chars, charsWritten: out _, format: "N");
                        Debug.Assert(result, "Guid.TryFormat not successful.");

                        instanceId = chars.Slice(0, 8).ToString();
                    }

                    return $"{item.ApplicationName}-{instanceId}";
                }
            }
        }

        return app.ApplicationName;
    }

    internal List<OtlpApplicationView> GetViews() => _applicationViews.Values.ToList();

    internal OtlpApplicationView GetView(RepeatedField<KeyValue> attributes)
    {
        // Inefficient to create this to possibly throw it away.
        var view = new OtlpApplicationView(this, attributes);

        if (_applicationViews.TryGetValue(view.Properties, out var applicationView))
        {
            return applicationView;
        }

        return _applicationViews.GetOrAdd(view.Properties, view);
    }

    /// <summary>
    /// Application views are equal when all properties are equal.
    /// </summary>
    private sealed class ApplicationViewKeyComparer : IEqualityComparer<KeyValuePair<string, string>[]>
    {
        public static readonly ApplicationViewKeyComparer Instance = new();

        public bool Equals(KeyValuePair<string, string>[]? x, KeyValuePair<string, string>[]? y)
        {
            if (x == y)
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }
            if (x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (!string.Equals(x[i].Key, y[i].Key, StringComparisons.OtlpAttribute))
                {
                    return false;
                }
                if (!string.Equals(x[i].Value, y[i].Value, StringComparisons.OtlpAttribute))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode([DisallowNull] KeyValuePair<string, string>[] obj)
        {
            var hashCode = new HashCode();
            for (var i = 0; i < obj.Length; i++)
            {
                hashCode.Add(StringComparers.OtlpAttribute.GetHashCode(obj[i].Key));
                hashCode.Add(StringComparers.OtlpAttribute.GetHashCode(obj[i].Value));
            }

            return hashCode.ToHashCode();
        }
    }
}
