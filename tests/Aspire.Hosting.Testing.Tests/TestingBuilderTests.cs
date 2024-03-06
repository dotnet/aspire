// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public class TestingBuilderTests
{
    [LocalOnlyFact]
    public async Task HasEndPoints()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Program>();
        await using var app = await appHost.BuildAsync();

        await app.StartAsync();

        // Get an endpoint from a resource
        var workerEndpoint = app.GetEndpoint("myworker1", "myendpoint1");
        Assert.NotNull(workerEndpoint);
        Assert.True(workerEndpoint.Host.Length > 0);

        // Get a connection string from a resource
        var pgConnectionString = app.GetConnectionString("postgres1");
        Assert.NotNull(pgConnectionString);
        Assert.True(pgConnectionString.Length > 0);
    }

    [LocalOnlyFact]
    public async Task CanGetResources()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Program>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Ensure that the resource which we added is present in the model.
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Contains(appModel.GetContainerResources(), c => c.Name == "redis1");
        Assert.Contains(appModel.GetProjectResources(), p => p.Name == "myworker1");
    }

    [LocalOnlyFact]
    public async Task HttpClientGetTest()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Program>();
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("mywebapp1");
        var result1 = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
        Assert.NotNull(result1);
        Assert.True(result1.Length > 0);
    }

    private sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
