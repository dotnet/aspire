// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Kubernetes;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

builder.AddDockerCompose("docker-compose");

builder.AddKubernetes("k8s");

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.AddAzurePublisher("azure");

#pragma warning restore ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var param0 = builder.AddParameter("param0");
var param1 = builder.AddParameter("param1", secret: true);
var param2 = builder.AddParameter("param2", "default", publishValueAsDefault: true);
var param3 = builder.AddParameter("param3", "default"); // Runtime only default value.

var azpgdb = builder.AddAzurePostgresFlexibleServer("azpg").RunAsContainer().AddDatabase("azdb");

var db = builder.AddPostgres("pg").AddDatabase("db");

var dbsetup = builder.AddProject<Projects.Publishers_DbSetup>("dbsetup")
                     .WithReference(db).WaitFor(db);

var backend = builder.AddProject<Projects.Publishers_ApiService>("api")
                     .WithExternalHttpEndpoints()
                     .WithHttpHealthCheck("/health")
                     .WithReference(db).WaitFor(db)
                     .WithReference(azpgdb).WaitFor(azpgdb)
                     .WaitForCompletion(dbsetup)
                     .WithReplicas(2);

// need a container to test.
var sqlServer = builder.AddSqlServer("sqlserver")
        .WithDataVolume("sqlserver-data");

var sqlDb = sqlServer.AddDatabase("sqldb");

builder.AddProject<Projects.Publishers_Frontend>("frontend")
       .WithReference(sqlDb)
       .WithEnvironment("P0", param0)
       .WithEnvironment("P1", param1)
       .WithEnvironment("P2", param2)
       .WithEnvironment("P3", param3)
       .WithReference(backend).WaitFor(backend);

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
