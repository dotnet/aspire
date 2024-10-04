// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

for (var i = 0; i < 10; i++)
{
    builder.AddTestResource($"test-{i:0000}");
}

var serviceBuilder = builder.AddProject<Projects.Stress_ApiService>("stress-apiservice", launchProfileName: null);
serviceBuilder.WithCommand(
    type: "icon-test",
    displayName: "Icon test",
    executeCommand: (c) =>
    {
        return Task.FromResult(CommandResults.Success());
    },
    iconName: "CloudDatabase");
serviceBuilder.WithCommand(
    type: "icon-test-highlighted",
    displayName: "Icon test highlighted",
    executeCommand: (c) =>
    {
        return Task.FromResult(CommandResults.Success());
    },
    iconName: "CloudDatabase",
    isHighlighted: true);

for (var i = 0; i < 30; i++)
{
    var port = 5180 + i;
    serviceBuilder.WithHttpEndpoint(port, name: $"http-{port}");
}

builder.AddProject<Projects.Stress_TelemetryService>("stress-telemetryservice");

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
