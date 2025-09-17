using Azure.Identity;
using AzureKusto.Worker;
using Kusto.Data;
using Kusto.Data.Net.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("testdb");

var connectionStringBuilder = new KustoConnectionStringBuilder(connectionString);

var clusterAddress = $"https://{connectionStringBuilder.ServiceName}.kusto.windows.net/";
var scope = $"{clusterAddress}.default";
var accessToken = await new DefaultAzureCredential().GetTokenAsync(new([scope]), CancellationToken.None);
var tokenValue = accessToken.Token;
connectionStringBuilder = connectionStringBuilder.WithAadApplicationTokenAuthentication(tokenValue);

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
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<IngestionWorker>();
}

var app = builder.Build();

app.Run();
