// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// using Aspire.Hosting.Azure;
// using Aspire.Hosting.Kubernetes;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Docker;
using Aspire.Hosting.Kubernetes;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerCompose("docker-compose", options => {
    options.DefaultContainerRegistry = "override.azurecr.io";
    // Do stuff here.
});

builder.AddKubernetes("k8s", options => {
    // Do stuff here.
});

builder.AddAzureContainerApps("aca", options => {
    // Do stuff here.
});

var db = builder.AddAzurePostgresFlexibleServer("pg")
                .WithPasswordAuthentication()
                .RunAsContainer(c =>
                {
                    c.WithPgAdmin(c =>
                    {
                        c.WithHostPort(15551);
                    });
                })
                .AddDatabase("db");

var dbsetup = builder.AddProject<Projects.Publishers_DbSetup>("dbsetup")
                     .WithReference(db).WaitFor(db);

var backend = builder.AddProject<Projects.Publishers_ApiService>("api")
                     .WithExternalHttpEndpoints()
                     .WithHttpHealthCheck("/health")
                     .WithReference(db).WaitFor(db)
                     .WaitForCompletion(dbsetup)
                     .WithReplicas(2);

// need a container to test.
builder.AddSqlServer("sqlserver")
        .WithDataVolume("sqlserver-data");

builder.AddProject<Projects.Publishers_Frontend>("frontend")
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
