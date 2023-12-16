// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

public static class OracleDatabaseExtensions
{
    public static void MapOracleDatabaseApi(this WebApplication app)
    {
        app.MapGet("/oracledatabase/verify", VerifyOracleDatabase);
    }

    private static IResult VerifyOracleDatabase(MyDbContext context)
    {
        try
        {
            var results = context.Database.SqlQueryRaw<int>("SELECT 1 FROM DUAL");
            return results.Any() ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
