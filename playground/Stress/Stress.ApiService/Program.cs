// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(ConsoleStresser.Stress);

app.MapGet("/", () => "Hello world");

app.Run();
