// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Aspire.Hosting.AWS.DynamoDBLocal;

/// <summary>
/// Utilities that can be used for helping to seed DynamoDB local with tables and data.
/// </summary>
public class SeedDynamoDBUtilities(IAmazonDynamoDB dynamoDBClient)
{
    /// <summary>
    /// DynamoDB client configured to send requests to DynamoDB local.
    /// </summary>
    public IAmazonDynamoDB DynamoDBClient { get; } = dynamoDBClient;

    /// <summary>
    /// Block until the DynamoDB local instance is available. That includes possibly pulling the
    /// image from the repository and starting it up.
    /// </summary>
    internal void WaitTillContainerAvailable(CancellationToken token)
    {
        Task.Run(async () =>
        {
            for(var attempt = 1; true; attempt++)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                var source = new CancellationTokenSource(3000);
                try
                {
                    await this.DynamoDBClient.ListTablesAsync(source.Token).ConfigureAwait(false);

                    // TODO: needs to be replaced with an ILogger when I figure out how to get an instance of ILogger in here.
                    Console.WriteLine("Table is available");
                    return;
                }
                catch(Exception)
                {
                }
            }
        }, token).Wait(token);
    }

    /// <summary>
    /// Returns true of the if the DynamoDB table exists.
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    public async Task<bool> CheckTableExists(string tableName)
    {
        await foreach(var name in (DynamoDBClient.Paginators.ListTables(new ListTablesRequest())).TableNames)
        {
            if(string.Equals(name, tableName, StringComparison.Ordinal))
            {
                return true;
            }    
        };

        return false;
    }
}
