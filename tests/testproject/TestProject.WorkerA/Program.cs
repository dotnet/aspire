// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestProject.WorkerA;

var builder = Host.CreateApplicationBuilder(args);
string? logPath = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
if (!string.IsNullOrEmpty(logPath))
{
    builder.Logging.AddFile(Path.Combine(logPath, "WorkerA.log"));
}
else
{
    throw new InvalidOperationException("TEST_LOG_PATH environment variable is not set.");
}
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
