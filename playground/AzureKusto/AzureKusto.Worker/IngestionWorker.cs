// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using Kusto.Ingest;
using Microsoft.Extensions.Options;
using Polly;

namespace AzureKusto.Worker;

internal sealed class IngestionWorker : BackgroundService
{
    private readonly IKustoIngestClient _ingestClient;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptions;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<IngestionWorker> _logger;

    public IngestionWorker(
        IKustoIngestClient ingestClient,
        IOptionsMonitor<WorkerOptions> workerOptions,
        [FromKeyedServices("kusto-resilience")] ResiliencePipeline pipeline,
        ILogger<IngestionWorker> logger)
    {
        _ingestClient = ingestClient;
        _workerOptions = workerOptions;
        _pipeline = pipeline;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting ingestion worker");

        // Option: Ingest as part of worker startup
        // This is another approach that is likely more versatile, where seeding occurs as part
        // of startup (optionally only in development).
        await IngestFromDataReaderAsync(stoppingToken);

        _logger.LogInformation("Ingestion complete");
        _workerOptions.CurrentValue.IsIngestionComplete = true;
    }

    private async Task IngestFromDataReaderAsync(CancellationToken stoppingToken)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));

        table.Rows.Add(7, "George");
        table.Rows.Add(8, "Henry");
        table.Rows.Add(9, "Isabel");

        var ingestionProps = new KustoIngestionProperties
        {
            DatabaseName = _workerOptions.CurrentValue.DatabaseName,
            TableName = _workerOptions.CurrentValue.TableName,
        };

        await _pipeline.ExecuteAsync(async ct =>
        {
            using var reader = table.CreateDataReader();
            await _ingestClient.IngestFromDataReaderAsync(reader, ingestionProps);
        }, stoppingToken);
    }
}
