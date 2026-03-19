// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.TryAddEventingSubscriber<TestResourceLifecycle>();

// Scenario 1: Keyed health checks (in-process)
// Each resource has multiple health checks registered directly in the AppHost.
// The dashboard shows each keyed health check as an individual entry.
AddTestResource("healthy", HealthStatus.Healthy, "I'm fine, thanks for asking.");
AddTestResource("unhealthy", HealthStatus.Unhealthy, "I can't do that, Dave.", exceptionMessage: "Feeling unhealthy.");
AddTestResource("degraded", HealthStatus.Degraded, "Had better days.", exceptionMessage: "Feeling degraded.");

// Scenario 2: Expanded HTTP health checks (using AspireHealthCheckResponseWriter)
// A single /health endpoint returns Aspire JSON format with multiple sub-entries.
// WithHttpHealthCheck() automatically detects the format and expands them into
// individual health check entries in the dashboard.
builder.AddProject<Projects.HealthChecksSandbox_ExpandedHealthService>("expanded-health")
    .WithHttpHealthCheck("/health").ExcludeFromManifest();

// Scenario 3: Plain HTTP health check (no Aspire response writer)
// A single /health endpoint returns the default ASP.NET Core response (just "Healthy"/"Unhealthy").
// WithHttpHealthCheck() can't parse sub-entries, so it shows as a single health check entry.
builder.AddProject<Projects.HealthChecksSandbox_PlainHealthService>("plain-health")
    .WithHttpHealthCheck("/health").ExcludeFromManifest();

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

void AddTestResource(string name, HealthStatus status, string? description = null, string? exceptionMessage = null)
{
    var hasHealthyAfterFirstRunCheckRun = false;
    builder.Services.AddHealthChecks()
                    .AddCheck(
                        $"{name}_check",
                        () => new HealthCheckResult(status, description, new InvalidOperationException(exceptionMessage))
                        )
                        .AddCheck($"{name}_resource_healthy_after_first_run_check", () =>
                        {
                            if (!hasHealthyAfterFirstRunCheckRun)
                            {
                                hasHealthyAfterFirstRunCheckRun = true;
                                return new HealthCheckResult(HealthStatus.Unhealthy, "Initial failure state.");
                            }

                            return new HealthCheckResult(HealthStatus.Healthy, "Healthy beginning second health check run.");
                        });

    builder
        .AddResource(new TestResource(name))
        .WithHealthCheck($"{name}_check")
        .WithHealthCheck($"{name}_resource_healthy_after_first_run_check")
        .WithInitialState(new()
        {
            ResourceType = "Test Resource",
            State = "Starting",
            Properties = [],
        })
        .ExcludeFromManifest();
}

internal sealed class TestResource(string name) : Resource(name);

internal sealed class TestResourceLifecycle(
    ResourceNotificationService notificationService) : IDistributedApplicationEventingSubscriber
{
    public Task OnBeforeStartAsync(BeforeStartEvent @event, CancellationToken cancellationToken)
    {
        foreach (var resource in @event.Model.Resources.OfType<TestResource>())
        {
            Task.Run(
                async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));

                    await notificationService.PublishUpdateAsync(
                        resource,
                        state => state with { State = new("Running", "success") });
                },
                cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        eventing.Subscribe<BeforeStartEvent>(OnBeforeStartAsync);
        return Task.CompletedTask;
    }
}
