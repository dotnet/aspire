// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

for (var i = 0; i < 10; i++)
{
    builder.AddTestResource($"test-{i:0000}");
}

var serviceBuilder = builder.AddProject<Projects.Stress_ApiService>("stress-apiservice", launchProfileName: null);
serviceBuilder.WithCommand(
    name: "icon-test",
    displayName: "Icon test",
    executeCommand: (c) =>
    {
        return Task.FromResult(CommandResults.Success());
    },
    iconName: "CloudDatabase");
serviceBuilder.WithCommand(
    name: "icon-test-highlighted",
    displayName: "Icon test highlighted",
    executeCommand: (c) =>
    {
        return Task.FromResult(CommandResults.Success());
    },
    iconName: "CloudDatabase",
    isHighlighted: true);

serviceBuilder.WithHttpEndpoint(5180, name: $"http");
for (var i = 1; i <= 30; i++)
{
    var port = 5180 + i;
    serviceBuilder.WithHttpEndpoint(port, name: $"http-{port}");
}

serviceBuilder.WithHttpCommand("/write-console", "Write to console", method: HttpMethod.Get, iconName: "ContentViewGalleryLightning");
serviceBuilder.WithHttpCommand("/increment-counter", "Increment counter", method: HttpMethod.Get, iconName: "ContentViewGalleryLightning");
serviceBuilder.WithHttpCommand("/big-trace", "Big trace", method: HttpMethod.Get, iconName: "ContentViewGalleryLightning");
serviceBuilder.WithHttpCommand("/trace-limit", "Trace limit", method: HttpMethod.Get, iconName: "ContentViewGalleryLightning");
serviceBuilder.WithHttpCommand("/log-message", "Log message", method: HttpMethod.Get, iconName: "ContentViewGalleryLightning");
serviceBuilder.WithHttpCommand("/log-message-limit", "Log message limit", method: HttpMethod.Get, iconName: "ContentViewGalleryLightning");

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
