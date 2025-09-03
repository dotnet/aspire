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
        // Option 2: Seed as part of worker startup
        // This is another approach that is likely more versatile, where seeding occurs as part of startup (optionally only in development).
        var command =
            $"""
            .execute database script with (ThrowOnErrors=true) <|
                 .create-merge table {_workerOptions.CurrentValue.TableName} (Id: int, Name: string, Timestamp: datetime)
                 .ingest inline into table {_workerOptions.CurrentValue.TableName} <|
                     11,"Alice",datetime(2024-01-01T10:00:00Z)
                     22,"Bob",datetime(2024-01-01T11:00:00Z)
                     33,"Charlie",datetime(2024-01-01T12:00:00Z)
            """;

        await _adminClient.ExecuteControlCommandAsync(_adminClient.DefaultDatabaseName, command);

        _logger.LogInformation("Ingestion complete");
        _workerOptions.CurrentValue.IsIngestionComplete = true;
    }
}
