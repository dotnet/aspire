// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);
string? logPath = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
if (logPath is not null)
{
    AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        File.WriteAllText(Path.Combine(logPath, "servicec-exception.log"), eventArgs.ExceptionObject.ToString());
}

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/pid", () => Environment.ProcessId);

app.Run();
