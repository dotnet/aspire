// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(args);
string? logPath = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
if (logPath is not null)
{
    File.WriteAllText(Path.Combine(logPath, "servicea-start.log"), "");
    AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        File.WriteAllText(Path.Combine(logPath, "servicea-exception.log"), eventArgs.ExceptionObject.ToString());
};
try
{
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/pid", () => Environment.ProcessId);

app.MapGet("/urls", (IServiceProvider sp) => sp.GetService<IServer>()?.Features?.Get<IServerAddressesFeature>()?.Addresses);

app.Run();
}
catch (Exception ex)
{
    if (logPath is not null)
    {
        File.WriteAllText(Path.Combine(logPath, "servicea-stop.log"), ex.ToString());
    }
    throw;
}
