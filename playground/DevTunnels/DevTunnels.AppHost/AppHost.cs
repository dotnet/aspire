// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.ApiService>("api");
var frontend = builder.AddProject<Projects.WebFrontEnd>("frontend");

var publicDevTunnel = builder.AddDevTunnel("devtunnel-public")
    .WithAnonymousAccess()
    .WithReference(frontend.GetEndpoint("https"));

var privateDevTunnel = builder.AddDevTunnel("devtunnel")
    .WithReference(frontend.GetEndpoint("https"));

// BUG: This currently causes an error because the env vars try to get written before the tunnel endpoint is allocated.
//      Manually starting the api after the tunnel is created works fine.
api.WithReference(publicDevTunnel.GetEndpoint(frontend, "https"));

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
