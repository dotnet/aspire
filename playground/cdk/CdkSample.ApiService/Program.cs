// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapGet("/", (IConfiguration config) =>
{
    return $"TABLE_URI is: {config["TABLE_URI"]}";
});

app.Run();
