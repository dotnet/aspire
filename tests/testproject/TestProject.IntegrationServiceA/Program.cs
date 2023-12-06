// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);

builder.AddSqlServerClient("sqlservercontainer");
builder.AddMySqlDataSource("mysqlcontainer");
builder.AddRedis("rediscontainer");
builder.AddNpgsqlDataSource("postgrescontainer");
builder.AddRabbitMQ("rabbitmqcontainer");
builder.AddMongoDBClient("mongodbcontainer");

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
app.Run();
