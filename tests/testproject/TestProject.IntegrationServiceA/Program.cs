// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestProject;

var builder = WebApplication.CreateBuilder(args);
string? skipResourcesValue = Environment.GetEnvironmentVariable("SKIP_RESOURCES");
var resourcesToSkip = !string.IsNullOrEmpty(skipResourcesValue)
                        ? TestResourceNamesExtensions.Parse(skipResourcesValue.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        : TestResourceNames.None;

if (!resourcesToSkip.HasFlag(TestResourceNames.redis))
{
    builder.AddKeyedRedisClient("redis");
}
if (!resourcesToSkip.HasFlag(TestResourceNames.postgres) || !resourcesToSkip.HasFlag(TestResourceNames.efnpgsql))
{
    builder.AddNpgsqlDataSource("postgresdb");
}
if (!resourcesToSkip.HasFlag(TestResourceNames.efnpgsql))
{
    builder.AddNpgsqlDbContext<NpgsqlDbContext>("postgresdb");
}

// Ensure healthChecks are added. Some components like Cosmos
// don't add this
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapGet("/", () => "Hello World!");

app.MapGet("/pid", () => Environment.ProcessId);

if (!resourcesToSkip.HasFlag(TestResourceNames.redis))
{
    app.MapRedisApi();
}

if (!resourcesToSkip.HasFlag(TestResourceNames.postgres))
{
    app.MapPostgresApi();
}
if (!resourcesToSkip.HasFlag(TestResourceNames.efnpgsql))
{
    app.MapNpgsqlEFCoreApi();
}

app.Run();
