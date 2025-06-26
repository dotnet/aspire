// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//using Aspire.Hosting.Tests.Utils;
//using Aspire.Hosting.Utils;
//using Microsoft.AspNetCore.InternalTesting;
//using Xunit;

//namespace Aspire.Hosting.Tests;

//public class ExternalServiceTests
//{
//    [Fact]
//    public void AddExternalServiceWithString()
//    {
//        using var builder = TestDistributedApplicationBuilder.Create();

//        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");

//        Assert.Equal("weather", externalService.Resource.Name);
//        Assert.Equal("https://api.weather.gov/", externalService.Resource.Url);
//    }

//    [Fact]
//    public void AddExternalServiceWithUri()
//    {
//        using var builder = TestDistributedApplicationBuilder.Create();

//        var uri = new Uri("https://api.weather.gov/");
//        var externalService = builder.AddExternalService("weather", uri);

//        Assert.Equal("weather", externalService.Resource.Name);
//        Assert.Equal("https://api.weather.gov/", externalService.Resource.Url);
//    }

//    [Fact]
//    public void AddExternalServiceThrowsWithInvalidUrl()
//    {
//        using var builder = TestDistributedApplicationBuilder.Create();

//        Assert.Throws<ArgumentException>(() => builder.AddExternalService("weather", "not-a-url"));
//    }

//    [Fact]
//    public void AddExternalServiceThrowsWithRelativeUri()
//    {
//        using var builder = TestDistributedApplicationBuilder.Create();

//        var relativeUri = new Uri("/relative", UriKind.Relative);
//        Assert.Throws<ArgumentException>(() => builder.AddExternalService("weather", relativeUri));
//    }

//    [Fact]
//    public async Task ExternalServiceCanBeReferenced()
//    {
//        using var builder = TestDistributedApplicationBuilder.Create();

//        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");
//        var project = builder.AddProject<TestProject>("project")
//                             .WithReference(externalService);

//        // Build the app to trigger environment variable setup
//        using var app = builder.Build();

//        // Call environment variable callbacks.
//        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

//        // Check that service discovery information was injected
//        Assert.Contains(config, kvp => kvp.Key.StartsWith("services__weather__"));
//        Assert.Contains(config, kvp => kvp.Key == "services__weather__https__0" && kvp.Value == "https://api.weather.gov:443");
//    }

//    [Fact]
//    public async Task ExternalServiceWithParameterExpression()
//    {
//        using var builder = TestDistributedApplicationBuilder.Create();

//        var urlParam = builder.AddParameter("weather-url", "https://api.weather.gov/");
//        var externalService = builder.AddExternalService("weather", ReferenceExpression.Create($"{urlParam}"));
//        var project = builder.AddProject<TestProject>("project")
//                             .WithReference(externalService);

//        // Build the app to trigger environment variable setup
//        using var app = builder.Build();

//        // Call environment variable callbacks.
//        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(project.Resource, DistributedApplicationOperation.Run, TestServiceProvider.Instance).DefaultTimeout();

//        // Check that service discovery information was injected
//        Assert.Contains(config, kvp => kvp.Key.StartsWith("services__weather__"));
//        // The value should be the parameter expression (with default scheme "http")
//        Assert.Contains(config, kvp => kvp.Key == "services__weather__http__0");
//    }

//    [Fact]
//    public void ExternalServiceEndpointCanBeReferenced()
//    {
//        using var builder = TestDistributedApplicationBuilder.Create();

//        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");
//        var endpoint = externalService.GetEndpoint("https");

//        Assert.NotNull(endpoint);
//        Assert.Equal("weather", endpoint.Resource.Name);
//        Assert.Equal("https", endpoint.EndpointName);
//        Assert.True(endpoint.IsAllocated);
//        // The URL should be accessible from the endpoint
//        Assert.Contains("https://api.weather.gov", endpoint.Url);
//    }

//    [Fact]
//    public void ExternalServiceWithEnvironmentReference()
//    {
//        using var builder = TestDistributedApplicationBuilder.Create();

//        var externalService = builder.AddExternalService("weather", "https://api.weather.gov/");
//        var project = builder.AddProject<TestProject>("project")
//                             .WithEnvironment("WEATHER_URL", externalService.GetEndpoint("https"));

//        // Verify that the environment variable was set with the endpoint reference
//        var envAnnotations = project.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();
//        Assert.NotEmpty(envAnnotations);
//    }

//    [Fact]
//    public void AddExternalServiceWithParameterOnly()
//    {
//        using var builder = TestDistributedApplicationBuilder.Create();

//        var externalService = builder.AddExternalService("weather");

//        Assert.Equal("weather", externalService.Resource.Name);
//        Assert.IsAssignableFrom<IResourceWithServiceDiscovery>(externalService.Resource);
//    }

//    private sealed class TestProject : IProjectMetadata
//    {
//        public string ProjectPath => "testproject";
//        public LaunchSettings LaunchSettings { get; } = new();
//    }
//}
