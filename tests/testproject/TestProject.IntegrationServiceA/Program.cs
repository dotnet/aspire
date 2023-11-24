// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);
builder.AddSqlServerClient("sqlserver");
builder.AddMySqlDataSource("mysql");
builder.AddRedis("redis");
builder.AddNpgsqlDataSource("postgres");
builder.AddRabbitMQ("rabbitmq");

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/", () => "Hello World!");
app.MapGet("/pid", () => Environment.ProcessId);
app.Run();
