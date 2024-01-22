// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Npgsql;

public static class PostgresExtensions
{
    public static void MapPostgresApi(this WebApplication app)
    {
        app.MapGet("/postgres/verify", VerifyPostgresAsync);
    }

    private static async Task<IResult> VerifyPostgresAsync(NpgsqlConnection connection)
    {
        try
        {
            await connection.OpenAsync();

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
