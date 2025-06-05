public class HttpClientWorker : BackgroundService
{
    private readonly ILogger<HttpClientWorker> _logger;
    private readonly IConfiguration _config;

    public HttpClientWorker(ILogger<HttpClientWorker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var targetUrl = _config["TARGET_URL"];

        using var httpClient = new HttpClient();
        using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, targetUrl), stoppingToken);

        response.EnsureSuccessStatusCode();

        var responseText = await response.Content.ReadAsStringAsync(stoppingToken);

        _logger.LogInformation("Received response from {Url}: {ResponseText}", targetUrl, responseText);
    }
}
