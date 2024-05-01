using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureEventHubProducerClient("eventhubns", settings =>
{
    settings.EventHubName = "hub";
    settings.Credential = new AzureCliCredential();
});

var app = builder.Build();

app.MapPost("/message", async (Stream body, EventHubProducerClient producerClient) =>
{
    var binaryData = await BinaryData.FromStreamAsync(body);

    await producerClient.SendAsync([new EventData(binaryData)]);

    return Results.Accepted();
});

app.MapDefaultEndpoints();

app.Run();
