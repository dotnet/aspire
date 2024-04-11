// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Aspire.TestProject;
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
        StringBuilder errorMessageBuilder = new();
        try
        {
            ResiliencePipeline pipeline = ResilienceUtils.GetDefaultResiliencePipelineBuilder<OracleException>(args =>
            {
                errorMessageBuilder.AppendLine($"{Environment.NewLine}Service retry #{args.AttemptNumber} due to {args.Outcome.Exception}");
                return ValueTask.CompletedTask;
            }).Build();

            return pipeline.Execute(() =>
            {
                var results = context.Database.SqlQueryRaw<int>("SELECT 1 FROM DUAL");
                return results.Any() ? Results.Ok("Success!") : Results.Problem("Failed");
            });
        }
        catch (Exception e)
        {
            return Results.Problem($"Error: {e}{Environment.NewLine}** Previous retries: {errorMessageBuilder}");
        }
    }
}
