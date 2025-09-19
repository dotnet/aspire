using Azure.Identity;
using AzureKusto.Worker;
using Kusto.Data;
using Kusto.Data.Net.Client;

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

builder.Services.AddOptions<WorkerOptions>();

builder.Services.AddHostedService<QueryWorker>();
builder.Services.AddHostedService<IngestionWorker>();

var app = builder.Build();

app.Run();
