using ServiceBusWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureServiceBusClient("sbemulator");

builder.Services.AddHostedService<Consumer>();
builder.Services.AddHostedService<Producer>();

var host = builder.Build();

await host.RunAsync();
