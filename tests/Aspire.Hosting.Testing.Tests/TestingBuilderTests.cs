// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using System.Reflection;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.Tests;
using Aspire.Hosting.Tests.Utils;
using Aspire.TestProject;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public class TestingBuilderTests
{
    [Fact]
    [RequiresDocker]
    public async Task CanLoadFromDirectoryOutsideOfAppContextBaseDirectory()
    {
        // This test depends on the TestProject.AppHost not being in `AppContext.BaseDirectory` for the tests assembly.
        var unexpectedAppHostFiles = Directory.GetFiles(AppContext.BaseDirectory, "TestProject.AppHost.*");
        if (unexpectedAppHostFiles.Length > 0)
        {
            // The test requires that the TestProject.AppHost* files not be present in the test directory
            // This is a defensive check to ensure that the test is not run in an unexpected environment due
            // to build changes
            throw new InvalidOperationException($"Found unexpected AppHost files in {AppContext.BaseDirectory}: {string.Join(", ", unexpectedAppHostFiles)}");
        }

        var testProjectAssemblyPath = Directory.GetFiles(
            Path.Combine(MSBuildUtils.GetRepoRoot(), "artifacts", "bin", "TestProject.AppHost"),
            "TestProject.AppHost.dll",
            SearchOption.AllDirectories).FirstOrDefault();

        Assert.True(File.Exists(testProjectAssemblyPath), $"TestProject.AppHost.dll not found at {testProjectAssemblyPath}.");

        var appHostAssembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, testProjectAssemblyPath));
        var appHostType = appHostAssembly.GetTypes().FirstOrDefault(t => t.Name.EndsWith("_AppHost"))
            ?? throw new InvalidOperationException("Generated AppHost type not found.");

        TestResourceNames resourcesToSkip = ~TestResourceNames.redis;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync(appHostType, ["--skip-resources", resourcesToSkip.ToCSVString()]);
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Sanity check that the app is running as expected
        // Get an endpoint from a resource
        var serviceAHttpEndpoint = app.GetEndpoint("servicea", "http");
        Assert.NotNull(serviceAHttpEndpoint);
        Assert.True(serviceAHttpEndpoint.Host.Length > 0);
    }

    [Fact]
    public async Task ThrowsForAssemblyWithoutAnEntrypoint()
    {
        var ioe = await Assert.ThrowsAsync<InvalidOperationException>(() => DistributedApplicationTestingBuilder.CreateAsync(typeof(Microsoft.Extensions.Logging.ConsoleLoggerExtensions)));
        Assert.Contains("does not have an entry point", ioe.Message);
    }

    [Theory]
    [RequiresDocker]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateAsyncWithOptions(bool genericEntryPoint)
    {
        var nonExistantRegistry = "non-existant-registry-azurecr.io";
        var testEnvironmentName = "TestFooEnvironment";
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (options, settings) =>
        {
            options.ContainerRegistryOverride = nonExistantRegistry;
            settings.EnvironmentName = testEnvironmentName;
        };

        var appHost = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>([], configureBuilder)
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost), [], configureBuilder));
        Assert.Equal(testEnvironmentName, appHost.Environment.EnvironmentName);

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        foreach (var resource in appModel.GetContainerResources())
        {
            var containerImageAnnotation = resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
            Assert.NotNull(containerImageAnnotation);

            Assert.Equal(nonExistantRegistry, containerImageAnnotation!.Registry);
        }
    }

    [Theory]
    [RequiresDocker]
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

    [Theory]
    [RequiresDocker]
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

    [Theory]
    [RequiresDocker]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpClientGetTest(bool genericEntryPoint)
    {
        var appHost = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Wait for the application to be ready
        await app.WaitForTextAsync("Application started.").WaitAsync(TimeSpan.FromMinutes(1));

        var httpClient = app.CreateHttpClientWithResilience("mywebapp1");
        var result1 = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
        Assert.NotNull(result1);
        Assert.True(result1.Length > 0);
    }

    [Theory]
    [RequiresDocker]
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

    [Theory]
    [RequiresDocker]
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

    [Theory]
    [RequiresDocker]
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

        // Wait for the application to be ready
        await app.WaitForTextAsync("Application started.").WaitAsync(TimeSpan.FromMinutes(1));

        // Explicitly get the HTTPS endpoint - this is only available on the "https" launch profile.
        var httpClient = app.CreateHttpClient("mywebapp1", "https");
        var result = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    // Tests that DistributedApplicationTestingBuilder throws exceptions at the right times when the app crashes.
    [Theory]
    [RequiresDocker]
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
                                : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost), [$"--crash-{crashArg}"], cts.Token).WaitAsync(cts.Token));
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
        app = await appHost.BuildAsync().WaitAsync(cts.Token);

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
