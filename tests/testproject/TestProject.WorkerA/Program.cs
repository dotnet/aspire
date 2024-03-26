// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using TestProject.WorkerA;

string? logPath = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
{
    Console.WriteLine("Unhandled exception: " + eventArgs.ExceptionObject);
    if (logPath is not null)
    {
        File.WriteAllText(Path.Combine(logPath, "IntegrationServiceA-exception.log"), eventArgs.ExceptionObject.ToString());
    }
};
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

using var host = builder.Build();
host.Run();
