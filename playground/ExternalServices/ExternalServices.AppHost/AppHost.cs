// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Yarp.Transforms;

var builder = DistributedApplication.CreateBuilder(args);

// URL value is in appsettings.json, or can be overridden by environment variable
var externalService = builder.AddExternalService("external-service", builder.AddParameter("external-service-url"));

var nuget = builder.AddExternalService("nuget", "https://api.nuget.org/")
    .WithHttpHealthCheck(path: "/v3/index.json");

var externalGateway = builder.AddYarp("gateway")
    .WithConfiguration(c =>
    {
        var nugetCluster = c.AddCluster(nuget);
        c.AddRoute("/nuget/{**catchall}", nugetCluster).WithTransformPathRemovePrefix("/nuget");
        c.AddRoute("/external-service/{**catchall}", externalService).WithTransformPathRemovePrefix("/external-service");
    });

builder.AddProject<Projects.WebFrontEnd>("frontend")
       .WithReference(nuget)
       .WithReference(externalService)
       .WithEnvironment("EXTERNAL_SERVICE_URL", externalService)
       .WithReference(externalGateway);

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
