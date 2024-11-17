using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Nats.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddNatsClient("nats", configureOptions: opts =>
{
    var jsonRegistry = new NatsJsonContextSerializerRegistry(AppJsonContext.Default);
    return opts with { SerializerRegistry = jsonRegistry };
});

builder.AddNatsJetStream();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapDefaultEndpoints();
app.MapGet("/ping", async (INatsConnection nats) =>
{
    var rtt = await nats.PingAsync();
    return Results.Json(new { rtt, nats.ServerInfo });
});

app.MapPost("/stream", async (StreamConfig config, INatsJSContext jetStream) =>
{
    var stream = await jetStream.CreateStreamAsync(config);
    var name = stream.Info.Config.Name;
    return Results.Created($"/stream/{name}", name);
});

app.MapGet("/stream/{name}", async (string name, INatsJSContext jetStream) =>
{
    var stream = await jetStream.GetStreamAsync(name);
    return Results.Ok(stream.Info);
});

app.MapPost("/publish/", async (AppEvent @event, INatsJSContext jetStream) =>
{
    try
    {
        var ack = await jetStream.PublishAsync(@event.Subject, @event);
        ack.EnsureSuccess();
    }
    catch (NatsJSPublishNoResponseException)
    {
        return Results.Problem("Make sure the stream is created before publishing.");
    }

    return Results.Created();
});

app.MapGet("/consume/{name}", async (string name, INatsJSContext jetStream) =>
{
    var stream = await jetStream.GetStreamAsync(name);
    var consumer = await stream.CreateOrderedConsumerAsync();

    var events = new List<AppEvent>();
    await foreach(var msg in consumer.ConsumeAsync<AppEvent>())
    {
        events.Add(msg.Data!);

        if (msg.Metadata?.NumPending == 0)
        {
            break;
        }
    }

    return Results.Ok(events);
});

app.Run();
