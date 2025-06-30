// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class ExternalServiceTests
{
    [Fact]
    public void AddExternalServiceWithString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");

        Assert.Equal("weather", externalService.Resource.Name);
        Assert.Equal("https://api.weather.gov/", externalService.Resource.Uri?.ToString());
        Assert.Null(externalService.Resource.UrlParameter);
    }

    [Fact]
    public void AddExternalServiceWithUri()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var uri = new Uri("https://api.weather.gov/");
        var externalService = builder.AddExternalService("weather", uri);

        Assert.Equal("weather", externalService.Resource.Name);
        Assert.Equal("https://api.weather.gov/", externalService.Resource.Uri?.ToString());
        Assert.Null(externalService.Resource.UrlParameter);
    }

    [Fact]
    public void AddExternalServiceWithParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var urlParam = builder.AddParameter("weather-url");
        var externalService = builder.AddExternalService("weather", urlParam);

        Assert.Equal("weather", externalService.Resource.Name);
        Assert.Null(externalService.Resource.Uri);
        Assert.NotNull(externalService.Resource.UrlParameter);
        Assert.Equal("weather-url", externalService.Resource.UrlParameter.Name);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("")]
    [InlineData("ftp://example.com/")]
    [InlineData("https://example.com/path")]
    [InlineData("https://example.com/path?query=value")]
    [InlineData("https://example.com#fragment")]
    public void AddExternalServiceThrowsWithInvalidUrl(string invalidUrl)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var ex = Assert.Throws<ArgumentException>(() => builder.AddExternalService("weather", invalidUrl));
        Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddExternalServiceThrowsWithRelativeUri()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var relativeUri = new Uri("/relative", UriKind.Relative);
        var ex = Assert.Throws<ArgumentException>(() => builder.AddExternalService("weather", relativeUri));
        Assert.Contains("absolute", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddExternalServiceThrowsWithUriWithPath()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var uriWithPath = new Uri("https://api.example.com/api/v1");
        var ex = Assert.Throws<ArgumentException>(() => builder.AddExternalService("weather", uriWithPath));
        Assert.Contains("absolute path must be \"/\"", ex.Message);
    }

    [Theory]
    [InlineData("https://api.weather.gov/")]
    [InlineData("http://localhost/")]
    [InlineData("https://example.com:8080/")]
    public void AddExternalServiceAcceptsValidUrls(string validUrl)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", validUrl);

        Assert.Equal("weather", externalService.Resource.Name);
        Assert.Equal(validUrl, externalService.Resource.Uri?.ToString());
    }

    [Fact]
    public async Task ExternalServiceWithHttpsCanBeReferenced()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        // Check that service discovery information was injected with https scheme
        Assert.Contains(config, kvp => kvp.Key == "services__weather__https__0" && kvp.Value == "https://api.weather.gov/");
    }

    [Fact]
    public async Task ExternalServiceWithHttpCanBeReferenced()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", "http://api.weather.gov/");
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        // Check that service discovery information was injected with http scheme
        Assert.Contains(config, kvp => kvp.Key == "services__weather__http__0" && kvp.Value == "http://api.weather.gov/");
    }

    [Fact]
    public async Task ExternalServiceWithParameterCanBeReferencedInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:weather-url"] = "https://api.weather.gov/";

        var urlParam = builder.AddParameter("weather-url");
        var externalService = builder.AddExternalService("weather", urlParam);
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        // Check that service discovery information was injected with the correct scheme from parameter value
        Assert.Contains(config, kvp => kvp.Key == "services__weather__https__0");
        // The value should be the parameter resource reference
        var httpsValue = config["services__weather__https__0"];
        Assert.IsType<ParameterResource>(httpsValue);
    }

    [Fact]
    public async Task ExternalServiceWithParameterCanBeReferencedInPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        
        var urlParam = builder.AddParameter("weather-url");
        var externalService = builder.AddExternalService("weather", urlParam);
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Publish, TestServiceProvider.Instance).DefaultTimeout();

        // In publish mode, scheme defaults to "default" since we can't validate the parameter value
        Assert.Contains(config, kvp => kvp.Key == "services__weather__default__0");
        var defaultValue = config["services__weather__default__0"];
        Assert.IsType<ParameterResource>(defaultValue);
    }

    [Fact]
    public async Task ExternalServiceWithInvalidParameterThrowsInRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:weather-url"] = "invalid-url";

        var urlParam = builder.AddParameter("weather-url");
        var externalService = builder.AddExternalService("weather", urlParam);
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

        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/")
                                    .WithHttpHealthCheck();

        // Build the app to register health checks
        using var app = builder.Build();

        // Verify that health check was registered
        var healthCheckService = app.Services.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }

    [Fact]
    public void ExternalServiceWithHttpHealthCheckCustomPath()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/")
                                    .WithHttpHealthCheck("/health", 200);

        // Build the app to register health checks
        using var app = builder.Build();

        // Verify that health check was registered
        var healthCheckService = app.Services.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        Assert.NotNull(healthCheckService);
    }

    [Fact]
    public void ExternalServiceWithHttpHealthCheckInvalidPath()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");

        // Should throw with invalid relative path
        Assert.Throws<ArgumentException>(() => externalService.WithHttpHealthCheck("https://invalid.com/path"));
    }

    [Fact]
    public void ExternalServiceResourceHasCorrectAnnotations()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");

        // Verify the resource has the expected annotations
        Assert.True(externalService.Resource.TryGetAnnotationsOfType<ResourceSnapshotAnnotation>(out var snapshotAnnotations));
        var snapshot = Assert.Single(snapshotAnnotations);
        Assert.Equal("ExternalService", snapshot.InitialSnapshot.ResourceType);
    }

    [Fact]
    public void ExternalServiceResourceImplementsExpectedInterfaces()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");

        // Verify the resource implements the expected interfaces
        Assert.IsAssignableFrom<IResourceWithoutLifetime>(externalService.Resource);
    }

    [Fact]
    public void ExternalServiceUrlValidationHelper()
    {
        // Test the static validation helper method
        Assert.True(ExternalServiceResource.UrlIsValidForExternalService("https://api.weather.gov/", out var uri, out var message));
        Assert.Equal("https://api.weather.gov/", uri!.ToString());
        Assert.Null(message);

        Assert.False(ExternalServiceResource.UrlIsValidForExternalService("invalid-url", out var invalidUri, out var invalidMessage));
        Assert.Null(invalidUri);
        Assert.NotNull(invalidMessage);
        Assert.Contains("absolute URI", invalidMessage);

        Assert.False(ExternalServiceResource.UrlIsValidForExternalService("https://api.weather.gov/path", out var pathUri, out var pathMessage));
        Assert.Null(pathUri);
        Assert.NotNull(pathMessage);
        Assert.Contains("absolute path must be \"/\"", pathMessage);
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "testproject";
        public LaunchSettings LaunchSettings { get; } = new();
    }
}
