// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Oracle.ManagedDataAccess.Client;

public static class OracleDatabaseExtensions
{
    public static void MapOracleDatabaseApi(this WebApplication app)
    {
        app.MapGet("/oracledatabase/verify", VerifyOracleDatabaseAsync);
    }

    private static async Task<IResult> VerifyOracleDatabaseAsync(OracleConnection connection)
    {
        try
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1 FROM DUAL";
            var results = await command.ExecuteReaderAsync();

            return results.HasRows ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
