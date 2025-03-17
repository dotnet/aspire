using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;

namespace WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConfigurationRefresher _refresher;
    private readonly IVariantFeatureManager _featureManager;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, IConfigurationRefresher refresher, IVariantFeatureManager featureManager)
    {
        _logger = logger;
        _configuration = configuration;
        _refresher = refresher;
        _featureManager = featureManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _refresher.TryRefreshAsync(stoppingToken);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Message from App Config: {message}", _configuration["message"]);
                _logger.LogInformation("Beta feature flag is enabled: {enabled}", await _featureManager.IsEnabledAsync("Beta", stoppingToken));
            }
            await Task.Delay(10000, stoppingToken);
        }
    }
}
