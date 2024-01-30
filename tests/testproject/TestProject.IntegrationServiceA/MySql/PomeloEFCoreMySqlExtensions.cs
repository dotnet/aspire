// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

public static class PomeloEFCoreMySqlExtensions
{
    public static void MapPomeloEFCoreMySqlApi(this WebApplication app)
    {
        app.MapGet("/pomelo/verify", VerifyPomeloEFCoreMySqlAsync);
    }

    private static IResult VerifyPomeloEFCoreMySqlAsync(PomeloDbContext dbContext)
    {
        try
        {
            var results = dbContext.Database.SqlQueryRaw<int>("SELECT 1");
            return results.Any() ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
