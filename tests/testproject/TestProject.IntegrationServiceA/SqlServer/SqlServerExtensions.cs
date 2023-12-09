// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.SqlClient;

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
            await connection.OpenAsync();

            var tableName = $"t" + Guid.NewGuid().ToString("n");
            var command = connection.CreateCommand();
            command.CommandText = $"""
                CREATE TABLE {tableName} (x INT);
                INSERT INTO {tableName} VALUES (1);
                SELECT x FROM {tableName};
                """;
            var results = await command.ExecuteReaderAsync();

            return results.HasRows ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
