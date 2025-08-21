using AzureKusto.Worker;
using Kusto.Data;
using Kusto.Data.Net.Client;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("testdb");
    return new KustoConnectionStringBuilder(connectionString);
});
builder.Services.AddSingleton(sp =>
{
    var kcsb = sp.GetRequiredService<KustoConnectionStringBuilder>();
    return KustoClientFactory.CreateCslQueryProvider(kcsb);
});
builder.Services.AddSingleton(sp =>
{
    var kcsb = sp.GetRequiredService<KustoConnectionStringBuilder>();
    return KustoClientFactory.CreateCslAdminProvider(kcsb);
});

builder.Services.AddOptions<WorkerOptions>();

builder.Services.AddHostedService<QueryWorker>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<IngestionWorker>();
}

var app = builder.Build();

app.Run();
