using NATS.Client.Core;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNats("nats");

var app = builder.Build();

app.MapGet("/", async (INatsConnection nats) => $"""
                                                 Hello NATS!
                                                 rtt: {await nats.PingAsync()}
                                                 {nats.ServerInfo}
                                                 """);

app.Run();
