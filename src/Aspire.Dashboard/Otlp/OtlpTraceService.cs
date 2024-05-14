// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Authentication;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Trace.V1;

namespace Aspire.Dashboard.Otlp;

[Authorize(Policy = OtlpAuthorization.PolicyName)]
[SkipStatusCodePages]
public class OtlpTraceService
{
    private readonly ILogger<OtlpTraceService> _logger;
    private readonly TelemetryRepository _telemetryRepository;

    public OtlpTraceService(ILogger<OtlpTraceService> logger, TelemetryRepository telemetryRepository)
    {
        _logger = logger;
        _telemetryRepository = telemetryRepository;
    }

    public Task<ExportTraceServiceResponse> Export(ExportTraceServiceRequest request)
    {
        var addContext = new AddContext();
        _telemetryRepository.AddTraces(addContext, request.ResourceSpans);

        return Task.FromResult(new ExportTraceServiceResponse
        {
            PartialSuccess = new ExportTracePartialSuccess
            {
                RejectedSpans = addContext.FailureCount
            }
        });
    }
}
