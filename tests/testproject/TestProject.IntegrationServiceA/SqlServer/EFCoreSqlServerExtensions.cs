// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

public static class EFCoreSqlServerExtensions
{
    public static void MapEFCoreSqlServerApi(this WebApplication app)
    {
        app.MapGet("/efsqlserver/verify", VerifyEFCoreSqlServerAsync);
    }

    private static async Task<IResult> VerifyEFCoreSqlServerAsync(EFCoreSqlServerDbContext dbContext)
    {
        try
        {
            await dbContext.Database.EnsureCreatedAsync();

            var entry = new Entry();
            await dbContext.Entries.AddAsync(entry);
            await dbContext.SaveChangesAsync();

            var entries = await dbContext.Entries.ToListAsync();
            return entries.Count == 1 ? Results.Ok("Success!") : Results.Problem($"Failed, got {entries.Count} entries");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }
    }
}
