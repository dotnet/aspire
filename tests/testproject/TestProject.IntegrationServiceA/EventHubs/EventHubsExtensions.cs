// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs.Consumer;

public static class EventHubsExtensions
{
    public static void MapEventHubsApi(this WebApplication app)
    {
        app.MapGet("/eventhubs/verify", VerifyEventHubsAsync);
    }

    private static async Task<IResult> VerifyEventHubsAsync(EventHubProducerClient producerClient, EventHubConsumerClient consumerClient)
    {
        try
        {
            // If no exception is thrown when awaited, the Event Hubs service has acknowledged
            // receipt and assumed responsibility for delivery of the set of events to its partition.
            await producerClient.SendAsync([new EventData(Encoding.UTF8.GetBytes("hello worlds"))]);

            await foreach (var partition in consumerClient.ReadEventsAsync(new ReadEventOptions { MaximumWaitTime = TimeSpan.FromSeconds(5) }))
            {
                return Results.Ok();
            }

            return Results.Problem("No events were read.");
        }
        catch (Exception e)
        {
            return Results.Problem($"Error: {e}{Environment.NewLine}**");
        }
    }
}
