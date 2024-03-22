// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);
string? logPath = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
if (!string.IsNullOrEmpty(logPath))
{
    builder.Logging.AddFile(Path.Combine(logPath, "ServiceB.log"));
}
else
{
    throw new InvalidOperationException("TEST_LOG_PATH environment variable is not set.");
}
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/pid", () => Environment.ProcessId);

app.Run();
