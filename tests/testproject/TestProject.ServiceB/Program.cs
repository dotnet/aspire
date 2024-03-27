// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = WebApplication.CreateBuilder(args);
string? logPath = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
if (logPath is not null)
{
    File.WriteAllText(Path.Combine(logPath, "serviceb-start.log"), "");
    AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        File.WriteAllText(Path.Combine(logPath, "serviceb-exception.log"), eventArgs.ExceptionObject.ToString());
}

try
{
if (!string.IsNullOrEmpty(logPath))
{
    builder.Logging.AddFile(Path.Combine(logPath, "serviceb.log"));
}
else
{
    throw new InvalidOperationException("TEST_LOG_PATH environment variable is not set.");
}
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/pid", () => Environment.ProcessId);

app.Run();

}
catch (Exception ex)
{
    if (logPath is not null)
    {
        File.WriteAllText(Path.Combine(logPath, "serviceb-stop.log"), ex.ToString());
    }
    throw;
}
