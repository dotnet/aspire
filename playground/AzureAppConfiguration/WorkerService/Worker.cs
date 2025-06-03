using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace WorkerService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<IConfigurationRefresher> _refreshers;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, IConfigurationRefresherProvider refresherProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _refreshers = refresherProvider.Refreshers;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Configuration values:");
        foreach (var pair in _configuration.AsEnumerable())
        {
            Console.WriteLine($"{pair.Key} = {pair.Value}");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var refresher in _refreshers)
            {
                await refresher.TryRefreshAsync(stoppingToken);
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Message: {message}", _configuration["Message"]);
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}
