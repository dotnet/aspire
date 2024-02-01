// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MySqlConnector;
using Polly;

public static class MySqlExtensions
{
    public static void MapMySqlApi(this WebApplication app)
    {
        app.MapGet("/mysql/verify", VerifyMySqlAsync);
    }

    private static async Task<IResult> VerifyMySqlAsync(MySqlConnection connection)
    {
        try
        {
            var policy = Policy
                .Handle<MySqlException>()
                // retry every second for 60 seconds
                .WaitAndRetryAsync(60, retryAttempt => TimeSpan.FromSeconds(1));

            await policy.ExecuteAsync(connection.OpenAsync);

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1";
            var results = await command.ExecuteReaderAsync();

            return results.HasRows ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
