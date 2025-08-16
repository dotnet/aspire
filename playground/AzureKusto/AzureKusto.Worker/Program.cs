
using Kusto.Cloud.Platform.Data;
using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Data.Common;
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

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.Run();

internal sealed class Worker : BackgroundService
{
    private readonly ICslQueryProvider _queryClient;
    private readonly ICslAdminProvider _adminClient;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly Microsoft.Extensions.Logging.ILogger<Worker> _logger;

    public Worker(
        ICslQueryProvider queryClient,
        ICslAdminProvider adminClient,
        IHostApplicationLifetime hostApplicationLifetime,
        Microsoft.Extensions.Logging.ILogger<Worker> logger)
    {
        _queryClient = queryClient;
        _adminClient = adminClient;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reader = await _queryClient.ExecuteQueryAsync(_queryClient.DefaultDatabaseName, ".show version", new ClientRequestProperties(), stoppingToken);
        var results = reader.ToJObjects().StringJoin(",");

        _logger.LogInformation("Query Results: {results}", results);

        await Task.Delay(1_000, stoppingToken);

        // When completed, the entire app host will stop.
        _hostApplicationLifetime.StopApplication();
    }
}
