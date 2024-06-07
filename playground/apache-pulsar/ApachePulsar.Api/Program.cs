// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DotPulsar;
using DotPulsar.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var services = builder.AddServiceDefaults().Services.AddEndpointsApiExplorer().AddSwaggerGen();

var pulsarConnection = new Uri(builder.Configuration.GetConnectionString("Pulsar")!);
var pulsarClient = PulsarClient
    .Builder()
    .ServiceUrl(pulsarConnection)
    .Build();

Console.WriteLine($"Pulsar connection string {pulsarConnection}");

var pulsarNamespace = "persistent://public/default";
var pingTopic = $"{pulsarNamespace}/ping-field";
var pongTopic = $"{pulsarNamespace}/pong-field";

// Each player plays (produces) a move (message) into opponents field (topic)
// Each player then responds to opponent moves (messages) being played into their field (topic)

var pingProducerB = pulsarClient.NewProducer(Schema.String).Topic(pongTopic);
var pingConsumerB = pulsarClient.NewConsumer(Schema.String).Topic(pingTopic).SubscriptionName("ping-player");

var pongProducerB = pulsarClient.NewProducer(Schema.String).Topic(pingTopic);
var pongConsumerB = pulsarClient.NewConsumer(Schema.String).Topic(pongTopic).SubscriptionName("pong-player");

services.AddSingleton<MatchCoordinator>();

services
    .AddSingleton<PingPlayer>()
    .AddHostedService(sp => sp.GetRequiredService<PingPlayer>())
    .AddKeyedSingleton(typeof(PingPlayer), (_, _) => pingProducerB)
    .AddKeyedSingleton(typeof(PingPlayer), (_, _) => pingConsumerB);

services
    .AddSingleton<PongPlayer>()
    .AddHostedService(sp => sp.GetRequiredService<PongPlayer>())
    .AddKeyedSingleton(typeof(PongPlayer), (_, _) => pongProducerB)
    .AddKeyedSingleton(typeof(PongPlayer), (_, _) => pongConsumerB);

var app = builder.Build();

app.UseSwagger().UseSwaggerUI();

app.MapPost("/match/start", async ([FromServices] PingPlayer p) => await p.SmackTheBall()).WithOpenApi();
app.MapPost("/match/stop", async ([FromServices] MatchCoordinator mc) => await mc.HaltMatch()).WithOpenApi();

app.MapGet("/ping-player/received", ([FromServices] PingPlayer p) => Results.Ok(p.ReceivedBalls)).WithOpenApi();
app.MapGet("/pong-player/received", ([FromServices] PongPlayer p) => Results.Ok(p.ReceivedBalls)).WithOpenApi();

app.Run();
