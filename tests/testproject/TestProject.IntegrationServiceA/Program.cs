// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);
builder.AddSqlServerClient("tempdb");
builder.AddMySqlDataSource("mysqldb");
builder.AddMySqlDbContext<PomeloDbContext>("mysqldb", settings => settings.ServerVersion = "8.2.0-mysql");
builder.AddRedis("redis");
builder.AddNpgsqlDataSource("postgresdb");
builder.AddRabbitMQ("rabbitmq");
builder.AddMongoDBClient("mymongodb");
builder.AddOracleDatabaseDbContext<MyDbContext>("freepdb1");
builder.AddKafkaProducer<string, string>("kafka");
builder.AddKafkaConsumer<string, string>("kafka", consumerBuilder =>
{
    consumerBuilder.Config.GroupId = "aspire-consumer-group";
    consumerBuilder.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
});

builder.AddAzureCosmosDB("cosmos");

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapGet("/", () => "Hello World!");

app.MapGet("/pid", () => Environment.ProcessId);

app.MapRedisApi();

app.MapMongoDBApi();

app.MapMySqlApi();

app.MapPomeloEFCoreMySqlApi();

app.MapPostgresApi();

app.MapSqlServerApi();

app.MapRabbitMQApi();

app.MapOracleDatabaseApi();

app.MapKafkaApi();

app.MapCosmosApi();

app.Run();
