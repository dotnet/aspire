// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public class TestingBuilderTests
{
    [LocalOnlyTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HasEndPoints(bool genericEntryPoint)
    {
        var appHost = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Get an endpoint from a resource
        var workerEndpoint = app.GetEndpoint("myworker1", "myendpoint1");
        Assert.NotNull(workerEndpoint);
        Assert.True(workerEndpoint.Host.Length > 0);

        // Get a connection string from a resource
        var pgConnectionString = await app.GetConnectionStringAsync("postgres1");
        Assert.NotNull(pgConnectionString);
        Assert.True(pgConnectionString.Length > 0);
    }

    [LocalOnlyTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanGetResources(bool genericEntryPoint)
    {
        var appHost = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Ensure that the resource which we added is present in the model.
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Contains(appModel.GetContainerResources(), c => c.Name == "redis1");
        Assert.Contains(appModel.GetProjectResources(), p => p.Name == "myworker1");
    }

    [LocalOnlyTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpClientGetTest(bool genericEntryPoint)
    {
        var appHost = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var httpClient = app.CreateHttpClient("mywebapp1");
        var result1 = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
        Assert.NotNull(result1);
        Assert.True(result1.Length > 0);
    }

    [LocalOnlyTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetHttpClientBeforeStart(bool genericEntryPoint)
    {
        var appHost = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        await using var app = await appHost.BuildAsync();
        Assert.Throws<InvalidOperationException>(() => app.CreateHttpClient("mywebapp1"));
    }

    [LocalOnlyTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetsCorrectContentRoot(bool genericEntryPoint)
    {
        var appHost = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var hostEnvironment = app.Services.GetRequiredService<IHostEnvironment>();
        Assert.Contains("TestingAppHost1", hostEnvironment.ContentRootPath);
    }

    [LocalOnlyTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectsFirstLaunchProfile(bool genericEntryPoint)
    {
        var appHost = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();
        var config = app.Services.GetRequiredService<IConfiguration>();
        var profileName = config["AppHost:DefaultLaunchProfileName"];
        Assert.Equal("https", profileName);

        // Explicitly get the HTTPS endpoint - this is only available on the "https" launch profile.
        var httpClient = app.CreateHttpClient("mywebapp1", "https");
        var result = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    // Tests that DistributedApplicationTestingBuilder throws exceptions at the right times when the app crashes.
    [LocalOnlyTheory]
    [InlineData(true, "before-build")]
    [InlineData(true, "after-build")]
    [InlineData(true, "after-start")]
    [InlineData(true, "after-shutdown")]
    [InlineData(false, "before-build")]
    [InlineData(false, "after-build")]
    [InlineData(false, "after-start")]
    [InlineData(false, "after-shutdown")]
    public async Task CrashTests(bool genericEntryPoint, string crashArg)
    {
        var timeout = TimeSpan.FromMinutes(5);
        using var cts = new CancellationTokenSource(timeout);
        DistributedApplication? app = null;

        IDistributedApplicationTestingBuilder appHost;
        if (crashArg == "before-build")
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            genericEntryPoint ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>([$"--crash-{crashArg}"], cts.Token).WaitAsync(cts.Token)
                                : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost), [$"--crash-{crashArg}"], cts.Token).WaitAsync(cts.Token))
                .ConfigureAwait(false);
            Assert.Contains(crashArg, exception.Message);
            return;
        }
        else
        {
            appHost = genericEntryPoint
                ? await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>([$"--crash-{crashArg}"], cts.Token).WaitAsync(cts.Token)
            : await DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost), [$"--crash-{crashArg}"], cts.Token).WaitAsync(cts.Token);
        }

        cts.CancelAfter(timeout);
        app = await appHost.BuildAsync().WaitAsync(cts.Token).ConfigureAwait(false);

        cts.CancelAfter(timeout);
        if (crashArg == "after-build")
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync().WaitAsync(cts.Token));
            Assert.Contains(crashArg, exception.Message);

            // DisposeAsync should throw the same exception.
            exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await app.DisposeAsync().AsTask().WaitAsync(cts.Token));
            Assert.Contains(crashArg, exception.Message);

            return;
        }
        else
        {
            await app.StartAsync().WaitAsync(cts.Token);
        }

        cts.CancelAfter(timeout);
        if (crashArg is "after-shutdown" or "after-start")
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await app.DisposeAsync().AsTask().WaitAsync(cts.Token));
            Assert.Contains(crashArg, exception.Message);
            return;
        }
        else
        {
            await app.DisposeAsync().AsTask().WaitAsync(cts.Token);
        }
    }

    private sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
