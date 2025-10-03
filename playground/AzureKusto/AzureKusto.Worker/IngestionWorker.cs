// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Data.Common;
using Microsoft.Extensions.Options;

namespace AzureKusto.Worker;

internal sealed class IngestionWorker : BackgroundService
{
    private readonly ICslAdminProvider _adminClient;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptions;
    private readonly ILogger<QueryWorker> _logger;

    public IngestionWorker(
        ICslAdminProvider adminClient,
        IOptionsMonitor<WorkerOptions> workerOptions,
        ILogger<QueryWorker> logger)
    {
        _adminClient = adminClient;
        _workerOptions = workerOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting ingestion worker");

        // Option 2: Seed as part of worker startup
        // This is another approach that is likely more versatile, where seeding occurs as part of startup (optionally only in development).
        var command =
            $"""
                 .create-merge table {_workerOptions.CurrentValue.TableName} (Id: int, Name: string, Timestamp: datetime)
            """;
        await _adminClient.ExecuteControlCommandAsync(_adminClient.DefaultDatabaseName, command);

        command =
            $"""
                 .ingest inline into table {_workerOptions.CurrentValue.TableName} <|
                     11,"Dave",datetime(2024-02-01T10:00:00Z)
                     22,"Eve",datetime(2024-02-02T11:00:00Z)
                     33,"Frank",datetime(2024-02-03T12:00:00Z)
            """;
        await _adminClient.ExecuteControlCommandAsync(_adminClient.DefaultDatabaseName, command);
        _logger.LogInformation("Ingestion complete");
        _workerOptions.CurrentValue.IsIngestionComplete = true;
    }
}
