using Azure.Messaging.ServiceBus;
using ServiceBusWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureServiceBusClient("queueOne");

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateSender("queue1");
});
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateProcessor("queue1", new ServiceBusProcessorOptions
    {
        MaxConcurrentCalls = 1, // Process one message at a time
        AutoCompleteMessages = true
    });
});

builder.Services.AddHostedService<Consumer>();
builder.Services.AddHostedService<Producer>();

var host = builder.Build();

await host.RunAsync();
