// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Aspire.Dashboard.Otlp;

public sealed class OtlpTraceService
{
    private readonly ILogger<OtlpTraceService> _logger;
    private readonly TelemetryRepository _telemetryRepository;

    public OtlpTraceService(ILogger<OtlpTraceService> logger, TelemetryRepository telemetryRepository)
    {
        _logger = logger;
        _telemetryRepository = telemetryRepository;
    }

    public ExportTraceServiceResponse Export(ExportTraceServiceRequest request)
    {
        var addContext = new AddContext();
        _telemetryRepository.AddTraces(addContext, request.ResourceSpans);

        _logger.LogDebug("Processed trace export. Success count: {SuccessCount}, failure count: {FailureCount}", addContext.SuccessCount, addContext.FailureCount);

        return new ExportTraceServiceResponse
        {
            PartialSuccess = new ExportTracePartialSuccess
            {
                RejectedSpans = addContext.FailureCount
            }
        };
    }
}
