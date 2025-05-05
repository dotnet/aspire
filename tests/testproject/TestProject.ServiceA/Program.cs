// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/get-only", () => Results.Ok());
app.MapPost("/status/{statusCode:int}", (int statusCode) => Results.StatusCode(statusCode));
app.MapGet("/pid", () => Environment.ProcessId);

app.MapGet("/urls", (IServiceProvider sp) => sp.GetService<IServer>()?.Features?.Get<IServerAddressesFeature>()?.Addresses);

app.Run();
