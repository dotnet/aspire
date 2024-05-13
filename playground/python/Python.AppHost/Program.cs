// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Python;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddPythonProject("script-only", "../script_only", "main.py");
builder.AddPythonProject("instrumented-script", "../instrumented_script", "main.py");
builder.AddFlaskProjectWithVirtualEnvironment("flask-app", "../flask_app", "main");
builder.AddFlaskProjectWithVirtualEnvironment("instrumented-flask-app", "../instrumented_flask_app", "main");

builder.Build().Run();
