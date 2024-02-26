// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);
// builder.Configuration.AddEnvironmentVariables();
string[] componentsToSkip = Array.Empty<string>();
if (Environment.GetEnvironmentVariable("SKIP_COMPONENTS") is string skipComponents && skipComponents.Length > 0)
{
    componentsToSkip = skipComponents.Split(',', StringSplitOptions.RemoveEmptyEntries);
}
if (!componentsToSkip.Contains("sqlserver", StringComparer.OrdinalIgnoreCase))
{
    builder.AddSqlServerClient("tempdb");
}
if (!componentsToSkip.Contains("mysql", StringComparer.OrdinalIgnoreCase))
{
    builder.AddMySqlDataSource("mysqldb");
    builder.AddMySqlDbContext<PomeloDbContext>("mysqldb", settings => settings.ServerVersion = "8.2.0-mysql");
}
if (!componentsToSkip.Contains("redis", StringComparer.OrdinalIgnoreCase))
{
    builder.AddRedis("redis");
}
if (!componentsToSkip.Contains("postgres", StringComparer.OrdinalIgnoreCase))
{
    builder.AddNpgsqlDataSource("postgresdb");
}
if (!componentsToSkip.Contains("rabbitmq", StringComparer.OrdinalIgnoreCase))
{
    builder.AddRabbitMQ("rabbitmq");
}
if (!componentsToSkip.Contains("mongodb", StringComparer.OrdinalIgnoreCase))
{
    builder.AddMongoDBClient("mymongodb");
}
if (!componentsToSkip.Contains("oracledatabase", StringComparer.OrdinalIgnoreCase))
{
    builder.AddOracleDatabaseDbContext<MyDbContext>("freepdb1");
}
if (!componentsToSkip.Contains("kafka", StringComparer.OrdinalIgnoreCase))
{
    builder.AddKafkaProducer<string, string>("kafka");
    builder.AddKafkaConsumer<string, string>("kafka", consumerBuilder =>
    {
        consumerBuilder.Config.GroupId = "aspire-consumer-group";
        consumerBuilder.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
    });
}

if (!componentsToSkip.Contains("cosmos", StringComparer.OrdinalIgnoreCase))
{
    builder.AddAzureCosmosDB("cosmos");
}

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapGet("/", () => "Hello World!");

app.MapGet("/pid", () => Environment.ProcessId);

if (!componentsToSkip.Contains("redis", StringComparer.OrdinalIgnoreCase))
{
    app.MapRedisApi();
}

if (!componentsToSkip.Contains("mongodb", StringComparer.OrdinalIgnoreCase))
{
    app.MapMongoDBApi();
}

if (!componentsToSkip.Contains("mysql", StringComparer.OrdinalIgnoreCase))
{
    app.MapMySqlApi();
}

if (!componentsToSkip.Contains("pomelo", StringComparer.OrdinalIgnoreCase))
{
    app.MapPomeloEFCoreMySqlApi();
}

if (!componentsToSkip.Contains("postgres", StringComparer.OrdinalIgnoreCase))
{
    app.MapPostgresApi();
}

if (!componentsToSkip.Contains("sqlserver", StringComparer.OrdinalIgnoreCase))
{
    app.MapSqlServerApi();
}

if (!componentsToSkip.Contains("rabbitmq", StringComparer.OrdinalIgnoreCase))
{
    app.MapRabbitMQApi();
}

if (!componentsToSkip.Contains("oracledatabase", StringComparer.OrdinalIgnoreCase))
{
    app.MapOracleDatabaseApi();
}

if (!componentsToSkip.Contains("kafka", StringComparer.OrdinalIgnoreCase))
{
    app.MapKafkaApi();
}

if (!componentsToSkip.Contains("cosmos", StringComparer.OrdinalIgnoreCase))
{
    app.MapCosmosApi();
}

app.Run();
