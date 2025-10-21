using Azure.Identity;
using AzureKusto.Worker;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Data.Net.Client;
using Kusto.Ingest;
using Polly;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("testdb");

var connectionStringBuilder = new KustoConnectionStringBuilder(connectionString);
if (connectionStringBuilder.DataSourceUri.Contains("kusto.windows.net"))
{
    connectionStringBuilder = connectionStringBuilder.WithAadAzureTokenCredentialsAuthentication(new DefaultAzureCredential());
}

builder.Services.AddSingleton(sp =>
{
    return KustoClientFactory.CreateCslQueryProvider(connectionStringBuilder);
});
builder.Services.AddSingleton(sp =>
{
    return KustoClientFactory.CreateCslAdminProvider(connectionStringBuilder);
});
builder.Services.AddSingleton(sp =>
{
    return KustoIngestFactory.CreateStreamingIngestClient(connectionStringBuilder);
});

builder.Services.AddResiliencePipeline("kusto-resilience", builder =>
    builder.AddRetry(new()
    {
        // Retry any non-permanent exceptions
        MaxRetryAttempts = 10,
        Delay = TimeSpan.FromMilliseconds(100),
        BackoffType = DelayBackoffType.Exponential,
        ShouldHandle = new PredicateBuilder().Handle<Exception>(e => e is ICloudPlatformException cpe && ! cpe.IsPermanent),
    })
);

builder.Services.AddOptions<WorkerOptions>();

builder.Services.AddHostedService<QueryWorker>();
builder.Services.AddHostedService<IngestionWorker>();

var app = builder.Build();

app.Run();
