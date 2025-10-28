// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGPYTHON001 // Test for experimental feature

var builder = DistributedApplication.CreateBuilder(args);

builder.AddPythonScript("script-only", "../script_only", "main.py");
builder.AddPythonScript("instrumented-script", "../instrumented_script", "main.py");

builder.AddPythonModule("fastapi-app", "../module_only", "uvicorn")
    .WithArgs("api:app", "--reload", "--host=0.0.0.0", "--port=8000")
    .WithHttpEndpoint(targetPort: 8000)
    .WithUvEnvironment();

// Run the same app on another port using uvicorn directly
builder.AddPythonExecutable("fastapi-uvicorn-app", "../module_only", "uvicorn", true)
    .WithArgs("api:app", "--reload", "--host=0.0.0.0", "--port=8001")
    .WithHttpEndpoint(targetPort: 8001);

// Flask app using the AddFlaskApp method
builder.AddFlaskApp("flask-app", "../flask_app", "app:create_app")
    .WithUvEnvironment();

// Uvicorn app using the AddUvicornApp method
builder.AddUvicornApp("uvicorn-app", "../uvicorn_app", "app:app")
    .WithUvEnvironment()
    .WithExternalHttpEndpoints();

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
