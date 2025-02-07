// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Json;
using System.Reflection;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.Tests;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Aspire.TestProject;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Testing.Tests;

public class TestingBuilderTests(ITestOutputHelper output)
{
    [Fact]
    public void TestingBuilderHasAllPropertiesFromRealBuilder()
    {
        var realBuilderProperties = typeof(IDistributedApplicationBuilder).GetProperties().Select(p => p.Name).ToList();
        var testBuilderProperties = typeof(IDistributedApplicationTestingBuilder).GetProperties().Select(p => p.Name).ToList();
        var missingProperties = realBuilderProperties.Except(testBuilderProperties).ToList();
        Assert.Empty(missingProperties);
    }

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
        var builder = await DistributedApplicationTestingBuilder.CreateAsync(appHostType, ["--skip-resources", resourcesToSkip.ToCSVString()]);
        builder.WithTestAndResourceLogging(output);
        await using var app = await builder.BuildAsync();
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

        var builder = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>([], configureBuilder)
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost), [], configureBuilder));
        builder.WithTestAndResourceLogging(output);
        Assert.Equal(testEnvironmentName, builder.Environment.EnvironmentName);

        await using var app = await builder.BuildAsync();
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
        var builder = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        builder.WithTestAndResourceLogging(output);
        await using var app = await builder.BuildAsync();
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
        var builder = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        builder.WithTestAndResourceLogging(output);
        await using var app = await builder.BuildAsync();
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
        var builder = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        builder.WithTestAndResourceLogging(output);
        await using var app = await builder.BuildAsync();
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
        var builder = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        builder.WithTestAndResourceLogging(output);
        await using var app = await builder.BuildAsync();
        Assert.Throws<InvalidOperationException>(() => app.CreateHttpClient("mywebapp1"));
    }

    /// <summary>
    /// Tests that arguments propagate into the application host.
    /// </summary>
    [Theory]
    [RequiresDocker]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ArgsPropagateToAppHostConfiguration(bool genericEntryPoint, bool directArgs)
    {
        string[] args = directArgs ? ["APP_HOST_ARG=42"] : [];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = directArgs switch
        {
            true => (_, _) => { },
            false => (dao, habs) => habs.Args = ["APP_HOST_ARG=42"]
        };

        IDistributedApplicationTestingBuilder builder;
        if (genericEntryPoint)
        {
            builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>(args, configureBuilder);
        }
        else
        {
            builder = await DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost), args, configureBuilder);
        }

        builder.WithTestAndResourceLogging(output);
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Wait for the application to be ready
        await app.WaitForTextAsync("Application started.").WaitAsync(TimeSpan.FromMinutes(1));

        var httpClient = app.CreateHttpClientWithResilience("mywebapp1");
        var appHostArg = await httpClient.GetStringAsync("/get-app-host-arg");
        Assert.NotNull(appHostArg);
        Assert.Equal("42", appHostArg);
    }

    /// <summary>
    /// Tests that arguments propagate into the application host.
    /// </summary>
    [Theory]
    [RequiresDocker]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ArgsPropagateToAppHostConfigurationAdHocBuilder(bool directArgs)
    {
        IDistributedApplicationTestingBuilder builder;
        if (directArgs)
        {
            builder = DistributedApplicationTestingBuilder.Create(["APP_HOST_ARG=42"]);
        }
        else
        {
            builder = DistributedApplicationTestingBuilder.Create([], (dao, habs) => habs.Args = ["APP_HOST_ARG=42"]);
        }

        builder.WithTestAndResourceLogging(output);
        builder.AddProject<Projects.TestingAppHost1_MyWebApp>("mywebapp1")
            .WithEnvironment("APP_HOST_ARG", builder.Configuration["APP_HOST_ARG"])
            .WithEnvironment("LAUNCH_PROFILE_VAR_FROM_APP_HOST", builder.Configuration["LAUNCH_PROFILE_VAR_FROM_APP_HOST"]);
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Wait for the application to be ready
        await app.WaitForTextAsync("Application started.").WaitAsync(TimeSpan.FromMinutes(1));

        var httpClient = app.CreateHttpClientWithResilience("mywebapp1");
        var appHostArg = await httpClient.GetStringAsync("/get-app-host-arg");
        Assert.NotNull(appHostArg);
        Assert.Equal("42", appHostArg);
    }

    /// <summary>
    /// Tests that setting the launch profile works and results in environment variables from the launch profile
    /// populating in configuration.
    /// </summary>
    [Theory]
    [RequiresDocker]
    [InlineData("http", false)]
    [InlineData("http", true)]
    [InlineData("https", false)]
    [InlineData("https", true)]
    public async Task CanOverrideLaunchProfileViaArgs(string launchProfileName, bool directArgs)
    {
        var arg = $"DOTNET_LAUNCH_PROFILE={launchProfileName}";
        string[] args;
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder;
        if (directArgs)
        {
            args = [arg];
            configureBuilder = (_, _) => { };
        }
        else
        {
            args = [];
            configureBuilder = (dao, habs) => habs.Args = [arg];
        }

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>(args, configureBuilder);
        builder.WithTestAndResourceLogging(output);
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Wait for the application to be ready
        await app.WaitForTextAsync("Application started.").WaitAsync(TimeSpan.FromMinutes(1));

        var httpClient = app.CreateHttpClientWithResilience("mywebapp1");
        var appHostArg = await httpClient.GetStringAsync("/get-launch-profile-var");
        Assert.NotNull(appHostArg);
        Assert.Equal($"it-is-{launchProfileName}", appHostArg);

        // Check that, aside from the launch profile, the app host loaded environment settings from its launch profile
        var appHostLaunchProfileVar = await httpClient.GetStringAsync("/get-launch-profile-var-from-app-host");
        Assert.NotNull(appHostLaunchProfileVar);
        Assert.Equal($"app-host-is-{launchProfileName}", appHostLaunchProfileVar);
    }

    /// <summary>
    /// Tests that setting the launch profile works and results in environment variables from the launch profile
    /// populating in configuration.
    /// </summary>
    [Theory]
    [RequiresDocker]
    [InlineData("http", false)]
    [InlineData("http", true)]
    [InlineData("https", false)]
    [InlineData("https", true)]
    public async Task CanOverrideLaunchProfileViaArgsAdHocBuilder(string launchProfileName, bool directArgs)
    {
        var arg = $"DOTNET_LAUNCH_PROFILE={launchProfileName}";
        string[] args;
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder;
        if (directArgs)
        {
            args = [arg];
            configureBuilder = (_, _) => { };
        }
        else
        {
            args = [];
            configureBuilder = (dao, habs) => habs.Args = [arg];
        }

        var builder = DistributedApplicationTestingBuilder.Create(args, configureBuilder);
        builder.WithTestAndResourceLogging(output);
        builder.AddProject<Projects.TestingAppHost1_MyWebApp>("mywebapp1")
            .WithEnvironment("LAUNCH_PROFILE_VAR_FROM_APP_HOST", builder.Configuration["LAUNCH_PROFILE_VAR_FROM_APP_HOST"]);
        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Wait for the application to be ready
        await app.WaitForTextAsync("Application started.").WaitAsync(TimeSpan.FromMinutes(1));

        var httpClient = app.CreateHttpClientWithResilience("mywebapp1");
        var appHostArg = await httpClient.GetStringAsync("/get-launch-profile-var");
        Assert.NotNull(appHostArg);
        Assert.Equal($"it-is-{launchProfileName}", appHostArg);

        // Check that, aside from the launch profile, the app host loaded environment settings from its launch profile
        var appHostLaunchProfileVar = await httpClient.GetStringAsync("/get-launch-profile-var-from-app-host");
        Assert.NotNull(appHostLaunchProfileVar);
        Assert.Equal($"app-host-is-{launchProfileName}", appHostLaunchProfileVar);
    }

    [Theory]
    [RequiresDocker]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetsCorrectContentRoot(bool genericEntryPoint)
    {
        var builder = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        builder.WithTestAndResourceLogging(output);
        await using var app = await builder.BuildAsync();
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
        var builder = await (genericEntryPoint
            ? DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>()
            : DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost)));
        builder.WithTestAndResourceLogging(output);
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        var config = app.Services.GetRequiredService<IConfiguration>();
        var profileName = config["DOTNET_LAUNCH_PROFILE"];
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

        IDistributedApplicationTestingBuilder builder;
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
            builder = genericEntryPoint
                ? await DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>([$"--crash-{crashArg}"], cts.Token).WaitAsync(cts.Token)
            : await DistributedApplicationTestingBuilder.CreateAsync(typeof(Projects.TestingAppHost1_AppHost), [$"--crash-{crashArg}"], cts.Token).WaitAsync(cts.Token);
        }

        cts.CancelAfter(timeout);
        builder.WithTestAndResourceLogging(output);
        app = await builder.BuildAsync().WaitAsync(cts.Token);

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
