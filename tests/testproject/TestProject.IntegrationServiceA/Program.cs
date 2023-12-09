// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);
builder.AddSqlServerClient("tempdb");
builder.AddMySqlDataSource("mysqldb");
builder.AddRedis("redis");
builder.AddNpgsqlDataSource("postgresdb");
builder.AddRabbitMQ("rabbitmq");
builder.AddMongoDBClient("mymongodb");

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapGet("/", () => "Hello World!");

app.MapGet("/pid", () => Environment.ProcessId);

app.MapRedisApi();

app.MapMongoMovieApi();

app.MapMySqlApi();

app.MapPostgresApi();

app.MapSqlServerApi();

app.MapRabbitMQApi();

app.Run();
