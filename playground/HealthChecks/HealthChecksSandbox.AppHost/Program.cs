// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.HealthChecks;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.TryAddEventingSubscriber<TestResourceLifecycle>();

// Scenario 1: Keyed health checks (in-process)
// Each resource has multiple health checks registered directly in the AppHost.
// The dashboard shows each keyed health check as an individual entry.
AddKeyedTestResource("healthy", HealthStatus.Healthy, "I'm fine, thanks for asking.");
AddKeyedTestResource("unhealthy", HealthStatus.Unhealthy, "I can't do that, Dave.", exceptionMessage: "Feeling unhealthy.");
AddKeyedTestResource("degraded", HealthStatus.Degraded, "Had better days.", exceptionMessage: "Feeling degraded.");

// Scenario 2: Expanded HTTP health checks (using AspireHealthCheckResponseWriter)
// A single /health endpoint returns Aspire JSON format with multiple sub-entries.
// WithHttpHealthCheck() automatically detects the format and expands them into
// individual health check entries in the dashboard.
AddHttpTestResource("expanded-health", port: 15201, useAspireResponseWriter: true);

// Scenario 3: Plain HTTP health check (no Aspire response writer)
// A single /health endpoint returns the default ASP.NET Core response (just "Healthy"/"Unhealthy").
// WithHttpHealthCheck() can't parse sub-entries, so it shows as a single health check entry.
AddHttpTestResource("plain-health", port: 15202, useAspireResponseWriter: false);

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

void AddKeyedTestResource(string name, HealthStatus status, string? description = null, string? exceptionMessage = null)
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

void AddHttpTestResource(string name, int port, bool useAspireResponseWriter)
{
    builder
        .AddResource(new HttpTestResource(name, port, useAspireResponseWriter))
        .WithEndpoint(port: port, targetPort: port, scheme: "http", name: "http", isProxied: false)
        .WithHttpHealthCheck("/health")
        .WithInitialState(new()
        {
            ResourceType = "HTTP Test Resource",
            State = "Starting",
            Properties = [],
        })
        .ExcludeFromManifest();
}

internal sealed class TestResource(string name) : Resource(name);

internal sealed class HttpTestResource(string name, int port, bool useAspireResponseWriter) : Resource(name), IResourceWithEndpoints
{
    public int Port => port;
    public bool UseAspireResponseWriter => useAspireResponseWriter;

    public static WebApplication CreateHealthCheckServer(int port, bool useAspireResponseWriter)
    {
        var webBuilder = WebApplication.CreateSlimBuilder();
        webBuilder.Services.AddHealthChecks()
            .AddCheck("database", () => HealthCheckResult.Healthy("Database connection is healthy"))
            .AddCheck("cache", () => HealthCheckResult.Degraded("Cache is slow but functional"))
            .AddCheck("message_queue", () => HealthCheckResult.Unhealthy("Message queue is unavailable"));

        var app = webBuilder.Build();

        if (useAspireResponseWriter)
        {
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = AspireHealthCheckResponseWriter.WriteResponse
            });
        }
        else
        {
            app.MapHealthChecks("/health");
        }

        app.Urls.Add($"http://localhost:{port}");
        return app;
    }
}

internal sealed class TestResourceLifecycle(
    ResourceNotificationService notificationService,
    IDistributedApplicationEventing eventing,
    IServiceProvider services) : IDistributedApplicationEventingSubscriber
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

        foreach (var resource in @event.Model.Resources.OfType<HttpTestResource>())
        {
            Task.Run(
                async () =>
                {
                    // Start the inline HTTP server for this resource.
                    var webApp = HttpTestResource.CreateHealthCheckServer(resource.Port, resource.UseAspireResponseWriter);
                    await webApp.StartAsync(cancellationToken);

                    // Manually allocate the endpoint (not DCP-managed).
                    var endpointAnnotation = resource.Annotations.OfType<EndpointAnnotation>().First();
                    endpointAnnotation.AllocatedEndpoint = new AllocatedEndpoint(endpointAnnotation, "localhost", resource.Port);

                    // Fire lifecycle events so WithHttpHealthCheck resolves the URI.
                    await eventing.PublishAsync(
                        new ResourceEndpointsAllocatedEvent(resource, services), cancellationToken);
                    await eventing.PublishAsync(
                        new BeforeResourceStartedEvent(resource, services), cancellationToken);

                    // Transition to Running so health monitoring begins.
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
