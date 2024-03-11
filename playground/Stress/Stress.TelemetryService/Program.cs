// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Stress.ApiService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TelemetryStresser>();

builder.AddServiceDefaults();

var app = builder.Build();

app.Run();
