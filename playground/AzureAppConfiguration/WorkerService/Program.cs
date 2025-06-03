using WorkerService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.AddAzureAppConfiguration("aspire-appconfig", configureOptions: options =>
{
    options.ConfigureRefresh(refresh =>
    {
        refresh.RegisterAll();
        refresh.SetRefreshInterval(TimeSpan.FromSeconds(10));
    });
});

var host = builder.Build();
host.Run();
