// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestProject.WorkerA;

string? logPath = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
if (logPath is not null)
{
    AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        File.WriteAllText(Path.Combine(logPath, "workloada-exception.log"), eventArgs.ExceptionObject.ToString());
}
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

using var host = builder.Build();
host.Run();
