// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

public static class SqlServerEFCoreExtensions
{
    public static void MapSqlServerEFCoreApi(this WebApplication app)
    {
        app.MapGet("/efsqlserver/verify", VerifySqlServerEFCoreAsync);
    }

    private static IResult VerifySqlServerEFCoreAsync(PomeloSqlServerDbContext dbContext)
    {
        try
        {
            var results = dbContext.Database.SqlQueryRaw<int>("SELECT 1");
            return results.Any() ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem($"foo-bar: {e}");
        }
    }
}
