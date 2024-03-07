// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestProject;

var builder = WebApplication.CreateBuilder(args);
string? skipResourcesValue = Environment.GetEnvironmentVariable("SKIP_RESOURCES");
var resourcesToSkip = !string.IsNullOrEmpty(skipResourcesValue)
                        ? TestResourceNamesExtensions.Parse(skipResourcesValue.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        : new HashSet<TestResourceNames>();

if (!resourcesToSkip.Contains(TestResourceNames.sqlserver))
{
    builder.AddSqlServerClient("tempdb");
}
if (!resourcesToSkip.Contains(TestResourceNames.mysql))
{
    builder.AddMySqlDataSource("mysqldb");
    if (!resourcesToSkip.Contains(TestResourceNames.pomelo))
    {
        builder.AddMySqlDbContext<PomeloDbContext>("mysqldb", settings => settings.ServerVersion = "8.2.0-mysql");
    }
}
if (!resourcesToSkip.Contains(TestResourceNames.redis))
{
    builder.AddRedisClient("redis");
}
if (!resourcesToSkip.Contains(TestResourceNames.postgres))
{
    builder.AddNpgsqlDataSource("postgresdb");
}
if (!resourcesToSkip.Contains(TestResourceNames.rabbitmq))
{
    builder.AddRabbitMQClient("rabbitmq");
}
if (!resourcesToSkip.Contains(TestResourceNames.mongodb))
{
    builder.AddMongoDBClient("mymongodb");
}
if (!resourcesToSkip.Contains(TestResourceNames.oracledatabase))
{
    builder.AddOracleDatabaseDbContext<MyDbContext>("freepdb1");
}
if (!resourcesToSkip.Contains(TestResourceNames.kafka))
{
    builder.AddKafkaProducer<string, string>("kafka");
    builder.AddKafkaConsumer<string, string>("kafka", consumerBuilder =>
    {
        consumerBuilder.Config.GroupId = "aspire-consumer-group";
        consumerBuilder.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
    });
}

if (!resourcesToSkip.Contains(TestResourceNames.cosmos))
{
    builder.AddAzureCosmosDbClient("cosmos");
}

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapGet("/", () => "Hello World!");

app.MapGet("/pid", () => Environment.ProcessId);

if (!resourcesToSkip.Contains(TestResourceNames.redis))
{
    app.MapRedisApi();
}

if (!resourcesToSkip.Contains(TestResourceNames.mongodb))
{
    app.MapMongoDBApi();
}

if (!resourcesToSkip.Contains(TestResourceNames.mysql))
{
    app.MapMySqlApi();
}

if (!resourcesToSkip.Contains(TestResourceNames.pomelo))
{
    app.MapPomeloEFCoreMySqlApi();
}

if (!resourcesToSkip.Contains(TestResourceNames.postgres))
{
    app.MapPostgresApi();
}

if (!resourcesToSkip.Contains(TestResourceNames.sqlserver))
{
    app.MapSqlServerApi();
}

if (!resourcesToSkip.Contains(TestResourceNames.rabbitmq))
{
    app.MapRabbitMQApi();
}

if (!resourcesToSkip.Contains(TestResourceNames.oracledatabase))
{
    app.MapOracleDatabaseApi();
}

if (!resourcesToSkip.Contains(TestResourceNames.kafka))
{
    app.MapKafkaApi();
}

if (!resourcesToSkip.Contains(TestResourceNames.cosmos))
{
    app.MapCosmosApi();
}

app.Run();
