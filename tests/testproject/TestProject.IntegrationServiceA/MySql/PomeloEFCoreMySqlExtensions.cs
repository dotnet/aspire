// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public static class PomeloEFCoreMySqlExtensions
{
    public static void MapPomeloEFCoreMySqlApi(this WebApplication app)
    {
        app.MapGet("/pomelo/verify", VerifyPomeloEFCoreMySqlAsync);
    }

    private static async Task<IResult> VerifyPomeloEFCoreMySqlAsync(PomeloDbContext dbContext)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync();

            return canConnect ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
