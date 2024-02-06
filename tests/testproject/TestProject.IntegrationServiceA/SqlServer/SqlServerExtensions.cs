// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.SqlClient;
using Polly;

public static class SqlServerExtensions
{
    public static void MapSqlServerApi(this WebApplication app)
    {
        app.MapGet("/sqlserver/verify", VerifySqlServerAsync);
    }

    private static async Task<IResult> VerifySqlServerAsync(SqlConnection connection)
    {
        try
        {
            var policy = Policy
                .Handle<SqlException>()
                // retry 60 times with a 1 second delay between retries
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
