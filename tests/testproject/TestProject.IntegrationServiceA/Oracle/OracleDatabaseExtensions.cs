// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Polly;

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
            var policy = Policy
                .Handle<OracleException>()
                // retry every second for 60 seconds
                .WaitAndRetry(60, retryAttempt => TimeSpan.FromSeconds(1));

            return policy.Execute(() =>
            {
                var results = context.Database.SqlQueryRaw<int>("SELECT 1 FROM DUAL");
                return results.Any() ? Results.Ok("Success!") : Results.Problem("Failed");
            });
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
