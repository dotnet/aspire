using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureQueueClient("queue");
builder.AddAzureBlobClient("blob");
builder.AddAzureEventHubProducerClient("eventhubs", static settings => settings.EventHubName = "myhub");
#if !SKIP_PROVISIONED_AZURE_RESOURCE
builder.AddAzureServiceBusClient("messaging");
#endif

builder.ConfigureFunctionsWebApplication();

var host = builder.Build();

host.Run();
