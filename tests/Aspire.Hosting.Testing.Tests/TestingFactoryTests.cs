// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public class TestingFactoryTests(DistributedApplicationFixture<Projects.TestingAppHost1_AppHost> fixture) : IClassFixture<DistributedApplicationFixture<Projects.TestingAppHost1_AppHost>>
{
    private readonly DistributedApplication _app = fixture.Application;

    [LocalOnlyFact]
    public async Task HasEndPoints()
    {
        // Get an endpoint from a resource
        var workerEndpoint = _app.GetEndpoint("myworker1", "myendpoint1");
        Assert.NotNull(workerEndpoint);
        Assert.True(workerEndpoint.Host.Length > 0);

        // Get a connection string from a resource
        var pgConnectionString = await _app.GetConnectionStringAsync("postgres1");
        Assert.NotNull(pgConnectionString);
        Assert.True(pgConnectionString.Length > 0);
    }

    [LocalOnlyFact]
    public void CanGetResources()
    {
        var appModel = _app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Contains(appModel.GetContainerResources(), c => c.Name == "redis1");
        Assert.Contains(appModel.GetProjectResources(), p => p.Name == "myworker1");
    }

    [LocalOnlyFact]
    public async Task HttpClientGetTest()
    {
        var httpClient = _app.CreateHttpClient("mywebapp1");
        var result1 = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
        Assert.NotNull(result1);
        Assert.True(result1.Length > 0);
    }

    [LocalOnlyFact]
    public void SetsCorrectContentRoot()
    {
        var appModel = _app.Services.GetRequiredService<IHostEnvironment>();
        Assert.Contains("TestingAppHost1", appModel.ContentRootPath);
    }

    [LocalOnlyFact]
    public async Task SelectsFirstLaunchProfile()
    {
        var config = _app.Services.GetRequiredService<IConfiguration>();
        var profileName = config["AppHost:DefaultLaunchProfileName"];
        Assert.Equal("https", profileName);

        // Explicitly get the HTTPS endpoint - this is only available on the "https" launch profile.
        var httpClient = _app.CreateHttpClient("mywebapp1", "https");
        var result = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    private sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
