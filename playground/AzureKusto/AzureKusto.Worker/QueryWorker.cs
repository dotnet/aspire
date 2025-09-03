// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Kusto.Cloud.Platform.Data;
using Kusto.Data.Common;
using Microsoft.Extensions.Options;

namespace AzureKusto.Worker;

internal sealed class QueryWorker : BackgroundService
{
    private readonly ICslQueryProvider _queryClient;
    private readonly IOptionsMonitor<WorkerOptions> _workerOptions;
    private readonly ILogger<QueryWorker> _logger;

    public QueryWorker(
        ICslQueryProvider queryClient,
        IOptionsMonitor<WorkerOptions> workerOptions,
        ILogger<QueryWorker> logger)
    {
        _queryClient = queryClient;
        _workerOptions = workerOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && !_workerOptions.CurrentValue.IsIngestionComplete)
        {
            // Wait for ingestion to complete
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        var reader = await _queryClient.ExecuteQueryAsync(_queryClient.DefaultDatabaseName, _workerOptions.CurrentValue.TableName, new ClientRequestProperties(), stoppingToken);
        var results = string.Join(",", reader.ToJObjects());

        _logger.LogInformation("Query Results: {results}", results);
    }
}
