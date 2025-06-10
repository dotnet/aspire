using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureEventHubProducerClient("eventhubOne");

var app = builder.Build();

app.MapGet("/test", async (EventHubProducerClient producerClient) =>
{
    var binaryData = BinaryData.FromString("Hello, from /test sent via producerClient");
    await producerClient.SendAsync([new EventData(binaryData)]);

    return Results.Ok();
});
app.MapPost("/message", async (Stream body, EventHubProducerClient producerClient) =>
{
    var binaryData = await BinaryData.FromStreamAsync(body);

    await producerClient.SendAsync([new EventData(binaryData)]);

    return Results.Accepted();
});

app.MapDefaultEndpoints();

app.Run();
