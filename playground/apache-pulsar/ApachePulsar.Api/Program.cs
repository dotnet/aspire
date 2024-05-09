// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DotPulsar;

var builder = WebApplication.CreateBuilder(args);

var services = builder
    .AddServiceDefaults()
    .Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

var pulsarConnection = new Uri(builder.Configuration.GetConnectionString("Pulsar")!);
Console.WriteLine($"Pulsar connection string {pulsarConnection}");

var client = PulsarClient
    .Builder()
    .ServiceUrl(pulsarConnection)
    .Build();

services.Register<PingPlayer>(client);
services.Register<PongPlayer>(client);

var app = builder.Build();

app.UseSwagger().UseSwaggerUI();

PlayerEndpoints.Map(app);

app.Run();
