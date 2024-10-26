// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGPYTHON001 // Test for experimental feature

var builder = DistributedApplication.CreateBuilder(args);

builder.AddPythonApp("script-only", "../script_only", "main.py");
builder.AddPythonApp("instrumented-script", "../instrumented_script", "main.py");

builder.Build().Run();
