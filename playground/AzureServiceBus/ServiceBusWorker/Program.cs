using ServiceBusWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureServiceBusClient("sbemulator");

Console.WriteLine("ServiceBus producer/consumer test");

builder.Services.AddHostedService<Consumer>();
Console.WriteLine("Starting Service Bus consumer...");

builder.Services.AddHostedService<Producer>();
Console.WriteLine("Starting Service Bus producer...");

var host = builder.Build();

await host.RunAsync();
