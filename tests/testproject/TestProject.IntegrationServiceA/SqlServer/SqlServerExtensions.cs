// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.TestProject;
using Microsoft.Data.SqlClient;
using Polly;

public static class SqlServerExtensions
{
    public static void MapSqlServerApi(this WebApplication app)
    {
        app.MapGet("/sqlserver/verify", VerifySqlServerAsync);
    }

    private static async Task<IResult> VerifySqlServerAsync(SqlConnection connection)
    {
        StringBuilder errorMessageBuilder = new();
        try
        {
            ResiliencePipeline pipeline = ResilienceUtils.GetDefaultResiliencePipelineBuilder<SqlException>(args =>
            {
                errorMessageBuilder.AppendLine($"{Environment.NewLine}Service retry #{args.AttemptNumber} due to {args.Outcome.Exception}");
                return ValueTask.CompletedTask;
            }).Build();

            await pipeline.ExecuteAsync(async token => await connection.OpenAsync(token));

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1";
            var results = await command.ExecuteReaderAsync();

            return results.HasRows ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem($"Error: {e}{Environment.NewLine}** Previous retries: {errorMessageBuilder}");
        }
    }
}
