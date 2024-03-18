// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

var builder = WebApplication.CreateBuilder(args);
string? logPath = Environment.GetEnvironmentVariable("TEST_LOG_PATH");
if (!string.IsNullOrEmpty(logPath))
{
    builder.Logging.AddFile(Path.Combine(logPath, "servicea.log"));
}
else
{
    throw new InvalidOperationException("TEST_LOG_PATH environment variable is not set.");
}
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/pid", () => Environment.ProcessId);

app.MapGet("/urls", (IServiceProvider sp) => sp.GetService<IServer>()?.Features?.Get<IServerAddressesFeature>()?.Addresses);

app.Run();
