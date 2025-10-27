// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;

namespace Aspire.Dashboard.Telemetry;

/// <summary>
/// For recording errors to telemetry. Can be injected into components that handle errors but we still want to record to telemetry.
/// </summary>
public interface ITelemetryErrorRecorder
{
    void RecordError(string message, Exception exception, bool writeToLogging = false);
}

public sealed class TelemetryErrorRecorder : ITelemetryErrorRecorder
{
    private readonly DashboardTelemetryService _telemetryService;
    private readonly ILogger<TelemetryErrorRecorder> _logger;

    public TelemetryErrorRecorder(DashboardTelemetryService telemetryService, ILogger<TelemetryErrorRecorder> logger)
    {
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public void RecordError(string message, Exception exception, bool writeToLogging = false)
    {
        if (writeToLogging)
        {
            _logger.LogError(exception, message);
        }

        _telemetryService.PostFault(
            TelemetryEventKeys.Error,
            $"{exception.GetType().FullName}: {exception.Message}",
            FaultSeverity.Critical,
            new Dictionary<string, AspireTelemetryProperty>
            {
                [TelemetryPropertyKeys.ExceptionType] = new AspireTelemetryProperty(exception.GetType().FullName!),
                [TelemetryPropertyKeys.ExceptionMessage] = new AspireTelemetryProperty(exception.Message),
                [TelemetryPropertyKeys.ExceptionStackTrace] = new AspireTelemetryProperty(exception.StackTrace ?? string.Empty),
                [TelemetryPropertyKeys.ExceptionRuntimeVersion] = new AspireTelemetryProperty(VersionHelpers.RuntimeVersion?.ToString() ?? string.Empty),
            }
        );
    }
}
