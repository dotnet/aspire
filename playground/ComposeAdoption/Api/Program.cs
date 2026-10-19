// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var redis = new Uri(configuration["REDIS_URL"] ?? throw new InvalidOperationException("REDIS_URL is required."));
    using var connection = new TcpClient();
    await connection.ConnectAsync(redis.Host, redis.Port, cancellationToken);
    await using var stream = connection.GetStream();
    // RESP INCR visits, shared with the preexisting Python service.
    await stream.WriteAsync("*2\r\n$4\r\nINCR\r\n$6\r\nvisits\r\n"u8.ToArray(), cancellationToken);
    using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
    var response = await reader.ReadLineAsync(cancellationToken);
    if (response is null || !response.StartsWith(':') || !long.TryParse(response.AsSpan(1), out var visits))
    {
        throw new InvalidOperationException($"Redis INCR failed: {response}");
    }
    return Results.Ok(new { implementation = "local-dotnet", visits });
});

app.Run();
