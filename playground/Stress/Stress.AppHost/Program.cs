// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = DistributedApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks().AddAsyncCheck("health-test", async (ct) =>
{
    await Task.Delay(5_000, ct);
    return HealthCheckResult.Healthy();
});

for (var i = 0; i < 5; i++)
{
    var name = $"test-{i:0000}";
    var rb = builder.AddTestResource(name);
    IResource parent = rb.Resource;

    for (int j = 0; j < 3; j++)
    {
        name = name + $"-n{j}";
        var nestedRb = builder.AddNestedResource(name, parent);
        parent = nestedRb.Resource;
    }
}

builder.AddParameter("testParameterResource", () => "value", secret: true);
builder.AddContainer("hiddenContainer", "alpine")
    .WithInitialState(new CustomResourceSnapshot
    {
        ResourceType = "CustomHiddenContainerType",
        Properties = [],
        IsHidden = true
    });

// TODO: OTEL env var can be removed when OTEL libraries are updated to 1.9.0
// See https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/RELEASENOTES.md#1100
var serviceBuilder = builder.AddProject<Projects.Stress_ApiService>("stress-apiservice", launchProfileName: null)
    .WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_METRICS_EMIT_OVERFLOW_ATTRIBUTE", "true");
serviceBuilder
    .WithEnvironment("HOST", $"{serviceBuilder.GetEndpoint("http").Property(EndpointProperty.Host)}")
    .WithEnvironment("PORT", $"{serviceBuilder.GetEndpoint("http").Property(EndpointProperty.Port)}")
    .WithEnvironment("URL", $"{serviceBuilder.GetEndpoint("http").Property(EndpointProperty.Url)}");
serviceBuilder.WithCommand(
    name: "icon-test",
    displayName: "Icon test",
    executeCommand: (c) =>
    {
        return Task.FromResult(CommandResults.Success());
    },
    commandOptions: new CommandOptions
    {
        IconName = "CloudDatabase"
    });
serviceBuilder.WithCommand(
    name: "icon-test-highlighted",
    displayName: "Icon test highlighted",
    executeCommand: (c) =>
    {
        return Task.FromResult(CommandResults.Success());
    },
    commandOptions: new CommandOptions
    {
        IconName = "CloudDatabase",
        IsHighlighted = true
    });

serviceBuilder.WithHttpEndpoint(5180, name: $"http");
for (var i = 1; i <= 30; i++)
{
    var port = 5180 + i;
    serviceBuilder.WithHttpEndpoint(port, name: $"http-{port}");
}

serviceBuilder.WithHttpCommand("/write-console", "Write to console", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/increment-counter", "Increment counter", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/big-trace", "Big trace", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/trace-limit", "Trace limit", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/log-message", "Log message", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/log-message-limit", "Log message limit", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/multiple-traces-linked", "Multiple traces linked", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/overflow-counter", "Overflow counter", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/nested-trace-spans", "Out of order nested spans", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });

builder.AddProject<Projects.Stress_TelemetryService>("stress-telemetryservice")
       .WithUrls(c => c.Urls.Add(new() { Url = "https://someplace.com", DisplayText = "Some place" }))
       .WithUrl("https://someotherplace.com/some-path", "Some other place");

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

IResourceBuilder<IResource>? previousResourceBuilder = null;

for (var i = 0; i < 3; i++)
{
    var resourceBuilder = builder.AddProject<Projects.Stress_Empty>($"empty-{i:0000}");
    if (previousResourceBuilder != null)
    {
        resourceBuilder.WaitFor(previousResourceBuilder);
        resourceBuilder.WithHealthCheck("health-test");
    }

    previousResourceBuilder = resourceBuilder;
}

builder.Build().Run();
