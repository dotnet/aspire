// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.SqlClient;

namespace CatalogService.Services.Ratings;

public class RatingsService(SqlConnection connection)
{
    private readonly SqlConnection _connection = connection;

    public async Task ApplyRatingsAsync(IEnumerable<CatalogItem> items)
    {
        try
        {
            await _connection.OpenAsync();

            // Just grab a random value from the SQL server backend to prove
            // that connectivity is working. No need to implement the actual
            // external rating system.
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT CEILING(RAND()*5)";
            var ratingResult = (double?)await command.ExecuteScalarAsync();

            foreach (var item in items)
            {
                item.ThirdPartyRating = (int)5;
            }
        }
        finally
        {
            await _connection.CloseAsync();
        }
    }
}
