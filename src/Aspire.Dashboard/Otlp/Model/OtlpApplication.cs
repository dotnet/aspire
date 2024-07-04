// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;

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

    private readonly ILogger _logger;
    private readonly TelemetryLimitOptions _options;

    public KeyValuePair<string, string>[] Properties { get; }

    public OtlpApplication(string name, string instanceId, Resource resource, ILogger logger, TelemetryLimitOptions options)
    {
        var properties = new List<KeyValuePair<string, string>>();
        foreach (var attribute in resource.Attributes)
        {
            switch (attribute.Key)
            {
                case SERVICE_NAME:
                case SERVICE_INSTANCE_ID:
                    // Values passed in via ctor and set to members. Don't add to properties collection.
                    break;
                default:
                    properties.Add(new KeyValuePair<string, string>(attribute.Key, attribute.Value.GetString()));
                    break;

            }
        }
        Properties = properties.ToArray();

        ApplicationName = name;
        InstanceId = instanceId;

        _logger = logger;
        _options = options;
    }

    public Dictionary<string, string> AllProperties()
    {
        var props = new Dictionary<string, string>();
        props.Add(SERVICE_NAME, ApplicationName);
        props.Add(SERVICE_INSTANCE_ID, InstanceId);

        foreach (var kv in Properties)
        {
            props.TryAdd(kv.Key, kv.Value);
        }

        return props;
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
                                Name = metric.Name,
                                Description = metric.Description,
                                Unit = metric.Unit,
                                Type = MapMetricType(metric.DataCase),
                                Parent = GetMeter(sm.Scope),
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

    public List<OtlpInstrument> GetInstrumentsSummary()
    {
        _metricsLock.EnterReadLock();

        try
        {
            var instruments = new List<OtlpInstrument>(_instruments.Count);
            foreach (var instrument in _instruments)
            {
                instruments.Add(OtlpInstrument.Clone(instrument.Value, cloneData: false, valuesStart: null, valuesEnd: null));
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
}
