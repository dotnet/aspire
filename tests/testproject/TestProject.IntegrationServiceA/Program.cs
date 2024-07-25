// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestProject;

var builder = WebApplication.CreateBuilder(args);
string? skipResourcesValue = Environment.GetEnvironmentVariable("SKIP_RESOURCES");
var resourcesToSkip = !string.IsNullOrEmpty(skipResourcesValue)
                        ? TestResourceNamesExtensions.Parse(skipResourcesValue.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        : TestResourceNames.None;

if (!resourcesToSkip.HasFlag(TestResourceNames.sqlserver))
{
    builder.AddSqlServerClient("tempdb");
}
if (!resourcesToSkip.HasFlag(TestResourceNames.efsqlserver))
{
    builder.AddSqlServerDbContext<EFCoreSqlServerDbContext>("tempdb");
}
if (!resourcesToSkip.HasFlag(TestResourceNames.redis))
{
    builder.AddKeyedRedisClient("redis");
}
if (!resourcesToSkip.HasFlag(TestResourceNames.garnet))
{
    builder.AddKeyedRedisClient("garnet");
}
if (!resourcesToSkip.HasFlag(TestResourceNames.postgres) || !resourcesToSkip.HasFlag(TestResourceNames.efnpgsql))
{
    builder.AddNpgsqlDataSource("postgresdb");
}
if (!resourcesToSkip.HasFlag(TestResourceNames.efnpgsql))
{
    builder.AddNpgsqlDbContext<NpgsqlDbContext>("postgresdb");
}
if (!resourcesToSkip.HasFlag(TestResourceNames.rabbitmq))
{
    builder.AddRabbitMQClient("rabbitmq");
}
if (!resourcesToSkip.HasFlag(TestResourceNames.eventhubs))
{
    builder.AddAzureEventHubProducerClient("eventhubsns", settings => settings.EventHubName = "hub");
    builder.AddAzureEventHubConsumerClient("eventhubsns", settings => settings.EventHubName = "hub");
}
if (!resourcesToSkip.HasFlag(TestResourceNames.oracledatabase))
{
    builder.AddOracleDatabaseDbContext<MyDbContext>("freepdb1");
}

if (!resourcesToSkip.HasFlag(TestResourceNames.cosmos) || !resourcesToSkip.HasFlag(TestResourceNames.efcosmos))
{
    builder.AddAzureCosmosClient("cosmos");
}

if (!resourcesToSkip.HasFlag(TestResourceNames.efcosmos))
{
    builder.AddCosmosDbContext<EFCoreCosmosDbContext>("cosmos", "cosmos");
}

if (!resourcesToSkip.HasFlag(TestResourceNames.eventhubs))
{
    builder.AddAzureEventHubProducerClient("eventhubns", settings =>
    {
        settings.EventHubName = "hub";
    });

    builder.AddAzureEventHubConsumerClient("eventhubns", settings =>
    {
        settings.EventHubName = "hub";
    });
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

if (!resourcesToSkip.HasFlag(TestResourceNames.garnet))
{
    app.MapGarnetApi();
}

if (!resourcesToSkip.HasFlag(TestResourceNames.postgres))
{
    app.MapPostgresApi();
}
if (!resourcesToSkip.HasFlag(TestResourceNames.efnpgsql))
{
    app.MapNpgsqlEFCoreApi();
}

if (!resourcesToSkip.HasFlag(TestResourceNames.sqlserver))
{
    app.MapSqlServerApi();
}

if (!resourcesToSkip.HasFlag(TestResourceNames.efsqlserver))
{
    app.MapEFCoreSqlServerApi();
}

if (!resourcesToSkip.HasFlag(TestResourceNames.rabbitmq))
{
    app.MapRabbitMQApi();
}

if (!resourcesToSkip.HasFlag(TestResourceNames.oracledatabase))
{
    app.MapOracleDatabaseApi();
}

if (!resourcesToSkip.HasFlag(TestResourceNames.cosmos))
{
    app.MapCosmosApi();
}

if (!resourcesToSkip.HasFlag(TestResourceNames.efcosmos))
{
    app.MapEFCoreCosmosApi();
}

if (!resourcesToSkip.HasFlag(TestResourceNames.eventhubs))
{
    app.MapEventHubsApi();
}

app.Run();
