using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsWebApplicationBuilder.CreateBuilder();

builder.AddServiceDefaults();
builder.AddAzureQueueClient("queue");
builder.AddAzureBlobClient("blob");
#if !SKIP_EVENTHUBS_EMULATION
builder.AddAzureEventHubProducerClient("eventhubs", static settings => settings.EventHubName = "myhub");
builder.AddAzureServiceBusClient("messaging");
#endif

builder.Services
    .AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseOtlpExporter();

var host = builder.Build();

host.Run();
