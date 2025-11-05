// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var db1 = builder.AddAzurePostgresFlexibleServer("pg")
                 .RunAsContainer()
                 .AddDatabase("db1");

// .NET 
builder.AddProject<Projects.PostgresEndToEnd_ApiService>("dotnet")
       .WithExternalHttpEndpoints()
       .WithReference(db1).WaitFor(db1);

// Python (Flask)
builder.AddPythonApp("pythonservice", "../PostgresEndToEnd.PythonService", "flask")
       .WithEnvironment("FLASK_APP", "app:app")
       .WithUv()
       .WithArgs(c =>
       {
           c.Args.Add("run");
           c.Args.Add("--host=0.0.0.0");
           c.Args.Add("--port=8002");
       })
       .WithHttpEndpoint(targetPort: 8002)
       .WithExternalHttpEndpoints()
       .WithReference(db1).WaitFor(db1);

// NodeJS (TypeScript)
builder.AddJavaScriptApp("nodeservice", "../PostgresEndToEnd.NodeService", "app.ts")
       .WithHttpEndpoint(env: "PORT")
       .WithReference(db1)
       .WaitFor(db1)
       .WithExternalHttpEndpoints();

// Java (Spark Framework)
var mvn = builder.AddExecutable("mvn-clean", OperatingSystem.IsWindows() ? "mvn.cmd" : "mvn", "../PostgresEndToEnd.JavaService", ["clean", "package", "-DskipTests"]);

var java = builder.AddExecutable("javaservice", "java", "../PostgresEndToEnd.JavaService", ["-jar", "target/javaservice-1.0.0.jar"])
       .WithHttpEndpoint(env: "PORT")
       .WaitFor(mvn)
       .WithReference(db1).WaitFor(db1)
       .WithExternalHttpEndpoints();

mvn.WithParentRelationship(java);

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
