// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("password", "a@b/c=d&e:f", secret: true);

var db1 = builder.AddPostgres("pg", password: password)
                 .AddDatabase("db1");

// 1- Invoking WithReference automatically renders environment variables for each connection property
builder.AddProject<Projects.PostgresEndToEnd_ApiService>("api")
       .WithReference(db1).WaitFor(db1)

// 4- WithEnvironment allows custom environment variable configuration
       .WithEnvironment(context =>
       {
           // Connection property references can also be used directly to configure environment variables
           var host = db1.Resource.GetConnectionProperty("Host");

           // Using exposed properties
           var port = db1.Resource.Parent.Port;

           // Configure a custom environment variable using the connection properties
           // When building URIs we should have tools for encoding/escaping values
           context.EnvironmentVariables["DB"] = ReferenceExpression.Create($"{host}:{port}");
       });

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
