// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.AddSeqEndpoint("seq");
builder.AddServiceDefaults();

var app = builder.Build();

ActivitySource source = new("MyApp.Source");

app.MapDefaultEndpoints();

app.MapGet("/", () =>
{
    var min = 1;
    var max = 10;
    app.Logger.LogInformation("Range is between {Min} and {Max}", min, max);

    using var activity = source.StartActivity("Chose {Number}");

    var number = Random.Shared.Next(min, max);
    activity?.SetTag("Number", number);

    return $"Your random number is {number}";
});

app.Run();
