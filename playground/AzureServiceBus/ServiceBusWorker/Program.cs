using Microsoft.Extensions.Azure;
using ServiceBusWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureServiceBusSender("queueOne");
builder.AddAzureServiceBusProcessor("queueOne", configureClientBuilder: clientBuilder =>
{
    clientBuilder.ConfigureOptions(options =>
    {
        options.MaxConcurrentCalls = 1; // Process one message at a time
        options.AutoCompleteMessages = true;
    });
});

builder.Services.AddHostedService<Consumer>();
builder.Services.AddHostedService<Producer>();

var host = builder.Build();

await host.RunAsync();
