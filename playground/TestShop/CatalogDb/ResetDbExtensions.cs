// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CatalogDb;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.Builder;

public static class ResetDbExtensions
{
    public static WebApplication MapResetDbEndpoint(this WebApplication app)
    {
        var resetDbKey = app.Configuration["DatabaseResetKey"];
        if (!string.IsNullOrEmpty(resetDbKey))
        {
            app.MapPost("/reset-db", async ([FromHeader(Name = "Authorization")] string? key, CatalogDbContext dbContext, CatalogDbInitializer dbInitializer, CancellationToken cancellationToken) =>
            {
                if (!string.Equals(key, $"Key {resetDbKey}", StringComparison.Ordinal))
                {
                    return Results.Unauthorized();
                }

                // Delete and recreate the database. This is useful for development scenarios to reset the database to its initial state.
                await dbContext.Database.EnsureDeletedAsync(cancellationToken);
                await dbInitializer.InitializeDatabaseAsync(dbContext, cancellationToken);

                return Results.Ok();
            });
        }
        return app;
    }
}
