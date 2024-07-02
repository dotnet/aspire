// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Storage;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace Aspire.Dashboard.Otlp;

public sealed class OtlpLogsService(ILogger<OtlpLogsService> logger, TelemetryRepository telemetryRepository)
{
    private readonly ILogger<OtlpLogsService> _logger = logger;
    private readonly TelemetryRepository _telemetryRepository = telemetryRepository;

    public ExportLogsServiceResponse Export(ExportLogsServiceRequest request)
    {
        var addContext = new AddContext();
        _telemetryRepository.AddLogs(addContext, request.ResourceLogs);

        _logger.LogDebug("Processed logs export. Failure count: {FailureCount}", addContext.FailureCount);

        return new ExportLogsServiceResponse
        {
            PartialSuccess = new ExportLogsPartialSuccess
            {
                RejectedLogRecords = addContext.FailureCount
            }
        };
    }
}
