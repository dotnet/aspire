using Microsoft.Extensions.Hosting;

var builder = FunctionsWebApplicationBuilder.CreateBuilder();

builder.AddServiceDefaults();
builder.AddAzureQueueClient("queue");
builder.AddAzureBlobClient("blob");
builder.AddAzureEventHubProducerClient("eventhubs", static settings => settings.EventHubName = "myhub");
#if !SKIP_PROVISIONED_AZURE_RESOURCE
builder.AddAzureServiceBusClient("messaging");
#endif

var host = builder.Build();

host.Run();
