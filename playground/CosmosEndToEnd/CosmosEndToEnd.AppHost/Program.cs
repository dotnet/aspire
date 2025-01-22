// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECOSMOS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var db = builder.AddAzureCosmosDB("cosmos")
                .WithDatabase("db", database => database.Containers.Add(new("entries", "/id")))
                .RunAsPreviewEmulator(e => e.WithDataExplorer());
#pragma warning restore ASPIRECOSMOS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.AddProject<Projects.CosmosEndToEnd_ApiService>("api")
       .WithExternalHttpEndpoints()
       .WithReference(db).WaitFor(db);

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
