// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

public static class PlayerEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/start-match", async ([FromServices] PingPlayer startPlayer, CancellationToken cancellation) =>
        {
            await startPlayer.SmackTheBall(cancellation);
            return Results.Ok();
        }).WithOpenApi();

        app.MapGet("/ping-player/received", ([FromServices] PingPlayer player) => Results.Ok((object?)player.ReceivedBalls)).WithOpenApi();
        app.MapGet("/pong-player/received", ([FromServices] PongPlayer player) => Results.Ok((object?)player.ReceivedBalls)).WithOpenApi();
    }
}
