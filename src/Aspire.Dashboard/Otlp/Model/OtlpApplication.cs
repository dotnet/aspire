// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;

namespace Aspire.Dashboard.Otlp.Model;

[DebuggerDisplay("ApplicationName = {ApplicationName}, InstanceId = {InstanceId}")]
public class OtlpApplication
{
    public const string SERVICE_NAME = "service.name";
    public const string SERVICE_INSTANCE_ID = "service.instance.id";

    public string ApplicationName { get; }
    public string InstanceId { get; }
    public int Suffix { get; }

    private readonly ReaderWriterLockSlim _metricsLock = new();
    private readonly Dictionary<string, OtlpMeter> _meters = new();

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
            //
            // NOTE: The service.instance.id value is a recommended attribute, but not required.
            //       See: https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/semantic_conventions/README.md#service-experimental
            //
            InstanceId = ApplicationName;
        }
        Suffix = applications.Where(a => a.Value.ApplicationName == ApplicationName).Count();
        _logger = logger;
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

    public string UniqueApplicationName => $"{ApplicationName}-{Suffix}";

    public string ShortApplicationName
    {
        get
        {
            var n = ApplicationName + Suffix.ToString(CultureInfo.InvariantCulture);
            return n.Length <= 10 ? n : $"{ApplicationName.Left(3)}…{ApplicationName.Right(5)}{Suffix}";
        }
    }

    public void AddMetrics(AddContext context, RepeatedField<ScopeMetrics> scopeMetrics)
    {
        _metricsLock.EnterWriteLock();

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
            _metricsLock.ExitWriteLock();
        }
    }
}
