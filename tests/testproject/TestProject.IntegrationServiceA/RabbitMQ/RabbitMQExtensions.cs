// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using RabbitMQ.Client;

public static class RabbitMQExtensions
{
    public static void MapRabbitMQApi(this WebApplication app)
    {
        app.MapGet("/rabbit/verify", VerifyRabbitMQ);
    }

    private static IResult VerifyRabbitMQ(IConnection connection)
    {
        try
        {
            using var channel = connection.CreateModel();
            const string queueName = "hello";
            channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            const string message = "Hello World!";
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: string.Empty, routingKey: queueName, basicProperties: null, body: body);
            var result = channel.BasicGet(queueName, true);

            return result.Body.Span.SequenceEqual(body) ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
