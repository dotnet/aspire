// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);
builder.AddSqlServerClient("tempdb");
builder.AddMySqlDataSource("mysqldb");
builder.AddRedis("rediscontainer");
builder.AddNpgsqlDataSource("postgresdb");
builder.AddRabbitMQ("rabbitmqcontainer");
builder.AddMongoDBClient("mymongodb");
builder.AddOracleDatabaseDbContext<MyDbContext>("freepdb1");

builder.AddKeyedSqlServerClient("sqlserverabstract");
builder.AddKeyedMySqlDataSource("mysqlabstract");
builder.AddKeyedRedis("redisabstract");
builder.AddKeyedNpgsqlDataSource("postgresabstract");
builder.AddKeyedRabbitMQ("rabbitmqabstract");
builder.AddKeyedMongoDBClient("mongodbabstract");

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapGet("/", () => "Hello World!");

app.MapGet("/pid", () => Environment.ProcessId);

app.MapRedisApi();

app.MapMongoDBApi();

app.MapMySqlApi();

app.MapPostgresApi();

app.MapSqlServerApi();

app.MapRabbitMQApi();

app.MapOracleDatabaseApi();

app.Run();
