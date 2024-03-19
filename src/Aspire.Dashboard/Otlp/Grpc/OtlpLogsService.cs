// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Authentication;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace Aspire.Dashboard.Otlp.Grpc;

[Authorize(Policy = OtlpAuthorization.PolicyName)]
[SkipStatusCodePages]
public class OtlpLogsService : LogsService.LogsServiceBase
{
    private readonly ILogger<OtlpLogsService> _logger;
    private readonly TelemetryRepository _telemetryRepository;

    public OtlpLogsService(ILogger<OtlpLogsService> logger, TelemetryRepository telemetryRepository)
    {
        _logger = logger;
        _telemetryRepository = telemetryRepository;
    }

    public override Task<ExportLogsServiceResponse> Export(ExportLogsServiceRequest request, ServerCallContext context)
    {
        var addContext = new AddContext();
        _telemetryRepository.AddLogs(addContext, request.ResourceLogs);

        return Task.FromResult(new ExportLogsServiceResponse
        {
            PartialSuccess = new ExportLogsPartialSuccess
            {
                RejectedLogRecords = addContext.FailureCount
            }
        });
    }
}
