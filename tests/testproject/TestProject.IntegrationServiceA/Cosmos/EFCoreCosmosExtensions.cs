// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public static class EFCoreCosmosExtensions
{
    public static void MapEFCoreCosmosApi(this WebApplication app)
    {
        app.MapGet("/efcosmos/verify", VerifyEFCoreCosmosAsync);
    }

    private static async Task<IResult> VerifyEFCoreCosmosAsync(EFCoreCosmosDbContext dbContext)
    {
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            dbContext.AddRange([new Entry(), new Entry()]);
            var count = await dbContext.SaveChangesAsync();
            return count == 2 ? Results.Ok() : Results.Problem($"Expected 2 entries but got {count}");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
