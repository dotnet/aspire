// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
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
        Assert.Equal("https://api.weather.gov/", externalService.Resource.UrlExpression.ValueExpression);
    }

    [Fact]
    public void AddExternalServiceWithUri()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var uri = new Uri("https://api.weather.gov/");
        var externalService = builder.AddExternalService("weather", uri);

        Assert.Equal("weather", externalService.Resource.Name);
        Assert.Equal("https://api.weather.gov/", externalService.Resource.UrlExpression.ValueExpression);
    }

    [Fact]
    public void AddExternalServiceThrowsWithInvalidUrl()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        Assert.Throws<ArgumentException>(() => builder.AddExternalService("weather", "not-a-url"));
    }

    [Fact]
    public void AddExternalServiceThrowsWithRelativeUri()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var relativeUri = new Uri("/relative", UriKind.Relative);
        Assert.Throws<ArgumentException>(() => builder.AddExternalService("weather", relativeUri));
    }

    [Fact]
    public async Task ExternalServiceCanBeReferenced()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        // Check that service discovery information was injected
        Assert.Contains(config, kvp => kvp.Key.StartsWith("services__weather__"));
    }

    [Fact]
    public async Task ExternalServiceWithParameterExpression()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var urlParam = builder.AddParameter("weather-url", "https://api.weather.gov/");
        var externalService = builder.AddExternalService("weather", ReferenceExpression.Create($"{urlParam}"));
        var project = builder.AddProject<TestProject>("project")
                             .WithReference(externalService);

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

        // Check that service discovery information was injected
        Assert.Contains(config, kvp => kvp.Key.StartsWith("services__weather__"));
    }

    [Fact]
    public void ExternalServiceEndpointCanBeReferenced()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");
        var endpoint = externalService.GetEndpoint("default");

        Assert.NotNull(endpoint);
        Assert.Equal("weather", endpoint.Resource.Name);
        Assert.Equal("default", endpoint.EndpointName);
        Assert.True(endpoint.IsAllocated);
        // For external services, the URL may include the port for AllocatedEndpoint format
        Assert.Contains("api.weather.gov", endpoint.Url);
        Assert.Contains("https", endpoint.Url);
    }

    [Fact]
    public void ExternalServiceWithEnvironmentReference()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");
        var project = builder.AddProject<TestProject>("project")
                             .WithEnvironment("WEATHER_URL", externalService.GetEndpoint("default"));

        // Verify that the environment variable was set with the endpoint reference
        var envAnnotations = project.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();
        Assert.NotEmpty(envAnnotations);
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "testproject";
        public LaunchSettings LaunchSettings { get; } = new();
    }
}