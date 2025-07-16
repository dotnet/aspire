// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Tests;

public class ExternalServiceTests
{
    [Fact]
    public void AddExternalServiceWithString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("nuget", "https://nuget.org/");

        Assert.Equal("nuget", externalService.Resource.Name);
        Assert.Equal("https://nuget.org/", externalService.Resource.Uri?.ToString());
        Assert.Null(externalService.Resource.UrlParameter);
    }

    [Fact]
    public void AddExternalServiceWithUri()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var uri = new Uri("https://nuget.org/");
        var externalService = builder.AddExternalService("nuget", uri);

        Assert.Equal("nuget", externalService.Resource.Name);
        Assert.Equal("https://nuget.org/", externalService.Resource.Uri?.ToString());
        Assert.Null(externalService.Resource.UrlParameter);
    }

    [Fact]
    public void AddExternalServiceWithParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var urlParam = builder.AddParameter("nuget-url");
        var externalService = builder.AddExternalService("nuget", urlParam);

        Assert.Equal("nuget", externalService.Resource.Name);
        Assert.Null(externalService.Resource.Uri);
        Assert.NotNull(externalService.Resource.UrlParameter);
        Assert.Equal("nuget-url", externalService.Resource.UrlParameter.Name);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("")]
    [InlineData("https://example.com/path")]
    [InlineData("https://example.com/path?query=value")]
    [InlineData("https://example.com#fragment")]
    public void AddExternalServiceThrowsWithInvalidUrl(string invalidUrl)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var ex = Assert.Throws<ArgumentException>(() => builder.AddExternalService("nuget", invalidUrl));
        Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddExternalServiceThrowsWithRelativeUri()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var relativeUri = new Uri("/relative", UriKind.Relative);
        var ex = Assert.Throws<ArgumentException>(() => builder.AddExternalService("nuget", relativeUri));
        Assert.Contains("absolute", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddExternalServiceThrowsWithUriWithPath()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var uriWithPath = new Uri("https://api.example.com/api/v1");
        var ex = Assert.Throws<ArgumentException>(() => builder.AddExternalService("nuget", uriWithPath));
        Assert.Contains("absolute path must be \"/\"", ex.Message);
    }

    [Theory]
    [InlineData("https://nuget.org/")]
    [InlineData("http://localhost/")]
    [InlineData("https://example.com:8080/")]
    public void AddExternalServiceAcceptsValidUrls(string validUrl)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("nuget", validUrl);

        Assert.Equal("nuget", externalService.Resource.Name);
        Assert.Equal(validUrl, externalService.Resource.Uri?.ToString());
    }

    [Fact]
    public async Task ExternalServiceWithHttpsCanBeReferenced()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("nuget", "https://nuget.org/");
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        // Check that service discovery information was injected with https scheme
        Assert.Contains(config, kvp => kvp.Key == "services__nuget__https__0" && kvp.Value == "https://nuget.org/");
    }

    [Fact]
    public async Task ExternalServiceWithHttpCanBeReferenced()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("nuget", "http://nuget.org/");
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        // Check that service discovery information was injected with http scheme
        Assert.Contains(config, kvp => kvp.Key == "services__nuget__http__0" && kvp.Value == "http://nuget.org/");
    }

    [Fact]
    public async Task ExternalServiceWithParameterCanBeReferencedInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:nuget-url"] = "https://nuget.org/";

        var urlParam = builder.AddParameter("nuget-url");
        var externalService = builder.AddExternalService("nuget", urlParam);
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        // Check that service discovery information was injected with the correct scheme from parameter value
        Assert.Contains(config, kvp => kvp.Key == "services__nuget__https__0");
        // The value should be the URL value from the parameter
        var urlValue = config["services__nuget__https__0"];
        Assert.Equal("https://nuget.org/", urlValue);
    }

    [Fact]
    public async Task ExternalServiceWithParameterCanBeReferencedInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        
        var urlParam = builder.AddParameter("nuget-url");
        var externalService = builder.AddExternalService("nuget", urlParam);
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        var app = builder.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Publish, app.Services).DefaultTimeout();

        // In publish mode, scheme defaults to "default" since we can't validate the parameter value
        Assert.Contains(config, kvp => kvp.Key == "services__nuget__default__0");
        var urlValue = config["services__nuget__default__0"];
        Assert.Equal(urlParam.Resource.ValueExpression, urlValue);
    }

    [Fact]
    public async Task ExternalServiceWithInvalidParameterThrowsInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:nuget-url"] = "invalid-url";

        var urlParam = builder.AddParameter("nuget-url");
        var externalService = builder.AddExternalService("nuget", urlParam);
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        // Should throw when trying to evaluate environment variables with invalid parameter value
        await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();
        });
    }

    [Fact]
    public void ExternalServiceWithHttpHealthCheck()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("nuget", "https://nuget.org/")
                                     .WithHttpHealthCheck();

        // Build the app to register health checks
        using var app = builder.Build();

        // Verify that health check was registered
        Assert.True(externalService.Resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var healthCheckAnnotations));
        Assert.NotNull(healthCheckAnnotations.FirstOrDefault(hc => hc.Key.StartsWith($"{externalService.Resource.Name}_external")));
    }

    [Fact]
    public void ExternalServiceWithHttpHealthCheckCustomPath()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("nuget", "https://nuget.org/")
                                     .WithHttpHealthCheck("/health", 200);

        // Build the app to register health checks
        using var app = builder.Build();

        // Verify that health check was registered
        Assert.True(externalService.Resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var healthCheckAnnotations));
        Assert.NotNull(healthCheckAnnotations.FirstOrDefault(hc => hc.Key.StartsWith($"{externalService.Resource.Name}_external")));
    }

    [Fact]
    public void ExternalServiceWithHttpHealthCheckInvalidPath()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("nuget", "https://nuget.org/");

        // Should throw with invalid relative path
        Assert.Throws<ArgumentException>(() => externalService.WithHttpHealthCheck(path: "https://invalid.com/path"));
    }

    [Fact]
    public void ExternalServiceResourceHasExpectedInitialState()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("nuget", "https://nuget.org/");

        // Verify the resource has the expected annotations
        Assert.True(externalService.Resource.TryGetAnnotationsOfType<ResourceSnapshotAnnotation>(out var snapshotAnnotations));
        var snapshot = Assert.Single(snapshotAnnotations);
        Assert.Equal("ExternalService", snapshot.InitialSnapshot.ResourceType);
    }

    [Fact]
    public void ExternalServiceResourceImplementsExpectedInterfaces()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("nuget", "https://nuget.org/");

        // Verify the resource implements the expected interfaces
        Assert.IsAssignableFrom<IResourceWithoutLifetime>(externalService.Resource);
    }

    [Fact]
    public void ExternalServiceResourceIsExcludedFromPublishingManifest()
    {
        //ManifestPublishingCallbackAnnotation
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("nuget", "https://nuget.org/");

        // Verify the resource has the expected annotations
        Assert.True(externalService.Resource.TryGetAnnotationsOfType<ManifestPublishingCallbackAnnotation>(out var manifestAnnotations));
        var annotation = Assert.Single(manifestAnnotations);
        Assert.Equal(ManifestPublishingCallbackAnnotation.Ignore, annotation);
    }

    [Fact]
    public void ExternalServiceUrlValidationHelper()
    {
        // Test the static validation helper method
        Assert.True(ExternalServiceResource.UrlIsValidForExternalService("https://nuget.org/", out var uri, out var message));
        Assert.Equal("https://nuget.org/", uri!.ToString());
        Assert.Null(message);

        Assert.False(ExternalServiceResource.UrlIsValidForExternalService("invalid-url", out var invalidUri, out var invalidMessage));
        Assert.Null(invalidUri);
        Assert.NotNull(invalidMessage);
        Assert.Contains("absolute URI", invalidMessage);

        Assert.False(ExternalServiceResource.UrlIsValidForExternalService("https://nuget.org/path", out var pathUri, out var pathMessage));
        Assert.Null(pathUri);
        Assert.NotNull(pathMessage);
        Assert.Contains("absolute path must be \"/\"", pathMessage);
    }

    [Fact]
    public async Task ExternalServiceWithParameterGetValueAsyncErrorMarksAsFailedToStart()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Create a parameter with a broken value callback
        var urlParam = builder.AddParameter("failing-url", () => throw new InvalidOperationException("Parameter resolution failed"));
        var externalService = builder.AddExternalService("external", urlParam);

        using var app = builder.Build();

        // Start the app to trigger InitializeResourceEvent
        var appStartTask = app.StartAsync();

        // Wait for the resource to be marked as FailedToStart
        var resourceEvent = await app.ResourceNotifications.WaitForResourceAsync(
            externalService.Resource.Name,
            e => e.Snapshot.State?.Text == KnownResourceStates.FailedToStart
        ).DefaultTimeout();

        // Verify the resource is in the correct state
        Assert.Equal(KnownResourceStates.FailedToStart, resourceEvent.Snapshot.State?.Text);

        await app.StopAsync();
        await appStartTask; // Ensure start completes
    }

    [Fact]
    public async Task ExternalServiceWithParameterInvalidUrlMarksAsFailedToStart()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Create a parameter that returns an invalid URL
        var urlParam = builder.AddParameter("invalid-url", () => "invalid-url-not-absolute");
        var externalService = builder.AddExternalService("external", urlParam);

        using var app = builder.Build();

        // Start the app to trigger InitializeResourceEvent
        var appStartTask = app.StartAsync();

        // Wait for the resource to be marked as FailedToStart
        var resourceEvent = await app.ResourceNotifications.WaitForResourceAsync(
            externalService.Resource.Name,
            e => e.Snapshot.State?.Text == KnownResourceStates.FailedToStart
        ).DefaultTimeout();

        // Verify the resource is in the correct state
        Assert.Equal(KnownResourceStates.FailedToStart, resourceEvent.Snapshot.State?.Text);

        await app.StopAsync();
        await appStartTask; // Ensure start completes
    }

    [Fact]
    public async Task ExternalServiceWithValidParameterMarksAsRunning()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        // Create a parameter that returns a valid URL
        var urlParam = builder.AddParameter("valid-url", () => "https://example.com/");
        var externalService = builder.AddExternalService("external", urlParam);

        using var app = builder.Build();

        // Start the app to trigger InitializeResourceEvent
        var appStartTask = app.StartAsync();

        // Wait for the resource to be marked as Running
        var resourceEvent = await app.ResourceNotifications.WaitForResourceAsync(
            externalService.Resource.Name,
            e => e.Snapshot.State?.Text == KnownResourceStates.Running
        ).DefaultTimeout();

        // Verify the resource is in the correct state
        Assert.Equal(KnownResourceStates.Running, resourceEvent.Snapshot.State?.Text);

        await app.StopAsync();
        await appStartTask; // Ensure start completes
    }

    [Fact]
    public void ExternalServiceWithParameterHttpHealthCheckRegistersCustomHealthCheck()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var urlParam = builder.AddParameter("external-url");
        var externalService = builder.AddExternalService("external", urlParam)
                                     .WithHttpHealthCheck();

        // Build the app to register health checks
        using var app = builder.Build();

        // Verify that health check was registered
        Assert.True(externalService.Resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var healthCheckAnnotations));
        var healthCheckAnnotation = healthCheckAnnotations.FirstOrDefault(hc => hc.Key.StartsWith($"{externalService.Resource.Name}_external"));
        Assert.NotNull(healthCheckAnnotation);

        // Verify that the custom health check is registered in DI
        var healthCheckService = app.Services.GetService<HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }

    [Fact]
    public void ExternalServiceWithStaticUrlHttpHealthCheckUsesUrlGroup()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("external", "https://example.com/")
                                     .WithHttpHealthCheck();

        // Build the app to register health checks
        using var app = builder.Build();

        // Verify that health check was registered
        Assert.True(externalService.Resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var healthCheckAnnotations));
        var healthCheckAnnotation = healthCheckAnnotations.FirstOrDefault(hc => hc.Key.StartsWith($"{externalService.Resource.Name}_external"));
        Assert.NotNull(healthCheckAnnotation);

        // Verify that health check service is available
        var healthCheckService = app.Services.GetService<HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }

    [Fact]
    public async Task ExternalServiceWithParameterHttpHealthCheckResolvesUrlAsync()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:external-url"] = "https://httpbin.org/";

        var urlParam = builder.AddParameter("external-url");
        var externalService = builder.AddExternalService("external", urlParam)
                                     .WithHttpHealthCheck("/status/200");

        using var app = builder.Build();

        // Get the health check service and run health checks
        var healthCheckService = app.Services.GetRequiredService<HealthCheckService>();
        
        // Find our specific health check key
        Assert.True(externalService.Resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var healthCheckAnnotations));
        var healthCheckKey = healthCheckAnnotations.First(hc => hc.Key.StartsWith($"{externalService.Resource.Name}_external")).Key;

        // Run the health check
        var result = await healthCheckService.CheckHealthAsync(
            registration => registration.Name == healthCheckKey,
            CancellationToken.None).DefaultTimeout();

        // The result should be healthy since we're using httpbin.org which should be accessible
        // However, in a test environment this might fail due to network issues, so we just check that it ran
        Assert.Contains(healthCheckKey, result.Entries.Keys);
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "testproject";
        public LaunchSettings LaunchSettings { get; } = new();
    }
}
