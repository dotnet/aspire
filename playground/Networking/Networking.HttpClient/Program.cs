var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<HttpClientWorker>();

var host = builder.Build();
host.Run();
