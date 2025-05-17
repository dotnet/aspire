using Microsoft.AspNetCore.Mvc;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Client.AzureManaged;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDurableTaskClient(
    clientBuilder =>
    {
        clientBuilder.UseDurableTaskScheduler(
            builder.Configuration.GetConnectionString("taskhub") ?? throw new InvalidOperationException("Scheduler connection string not configured."),
            options =>
            {
                options.AllowInsecureCredentials = true;
            });
    });

var app = builder.Build();

app.MapPost("/create", async ([FromBody] EchoValue value, [FromServices] DurableTaskClient durableTaskClient) =>
    {
        string instanceId = await durableTaskClient.ScheduleNewOrchestrationInstanceAsync(
            "Echo",
            value);

        await durableTaskClient.WaitForInstanceCompletionAsync(instanceId);

        return Results.Ok();
    })
    .WithName("CreateOrchestration");

app.MapPost("/echo", ([FromBody] EchoValue value) =>
    {
        return new EchoValue { Text = $"Echoed: {value.Text}" };
    })
    .WithName("EchoText");

app.Run();

public record EchoValue
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
