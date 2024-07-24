// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Workload.Tests;
using Microsoft.Extensions.DependencyInjection;
using SamplesIntegrationTests;
using SamplesIntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Playground.Tests;

public class AppHostTests
{
    private readonly TestOutputWrapper _testOutput;
    public AppHostTests(ITestOutputHelper testOutput)
    {
        this._testOutput = new TestOutputWrapper(testOutput);
    }

    [Theory]
    [MemberData(nameof(AppHostAssemblies))]
    public async Task AppHostRunsCleanly(string appHostPath)
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(appHostPath, _testOutput);
        await using var app = await appHost.BuildAsync();

        var appHostLogs = app.GetAppHostLogs();
        var resourceLogs = app.GetResourceLogs();

        await app.StartAsync(waitForResourcesToStart: true);

        appHostLogs.EnsureNoErrors(cat => ShouldAssertInAppHostIgnoringResources(Path.GetFileNameWithoutExtension(appHostPath), cat));
        resourceLogs.EnsureNoErrors(ShouldAssertErrorsForResource);

        await app.StopAsync();
    }

    [Theory]
    [MemberData(nameof(TestEndpoints))]
    public async Task TestEndpointsReturnOk(TestEndpoints testEndpoints)
    {
        var appHostName = testEndpoints.AppHost!;
        var resourceEndpoints = testEndpoints.ResourceEndpoints!;

        var appHostPath = $"{appHostName}.dll";
        var appHost = await DistributedApplicationTestFactory.CreateAsync(appHostPath, _testOutput);
        var projects = appHost.Resources.OfType<ProjectResource>();
        await using var app = await appHost.BuildAsync();

        var appHostLogs = app.GetAppHostLogs();
        var resourceLogs = app.GetResourceLogs();

        await app.StartAsync(waitForResourcesToStart: true);

        if (testEndpoints.WaitForResources?.Count > 0)
        {
            // Wait until each resource transitions to the required state
            var timeout = TimeSpan.FromMinutes(5);
            foreach (var (ResourceName, TargetState) in testEndpoints.WaitForResources)
            {
                _testOutput.WriteLine($"Waiting for resource '{ResourceName}' to reach state '{TargetState}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}'");
                await app.WaitForResource(ResourceName, TargetState, new CancellationTokenSource(timeout).Token);
            }
        }

        foreach (var resource in resourceEndpoints.Keys)
        {
            var endpoints = resourceEndpoints[resource];

            if (endpoints.Count == 0)
            {
                // No test endpoints so ignore this resource
                continue;
            }

            HttpResponseMessage? response = null;

            using var client = app.CreateHttpClient(resource, null, clientBuilder =>
            {
                clientBuilder
                    .ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan)
                    .AddStandardResilienceHandler(resilience =>
                    {
                        resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(300);
                        resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                        resilience.Retry.MaxRetryAttempts = 30;
                        resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(300);
                    });
            });

            foreach (var path in endpoints)
            {
                _testOutput.WriteLine($"Calling endpoint '{client.BaseAddress}{path.TrimStart('/')} for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}'");
                response = await client.GetAsync(path);

                Assert.True(HttpStatusCode.OK == response.StatusCode, $"Endpoint '{client.BaseAddress}{path.TrimStart('/')}' for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}' returned status code {response.StatusCode}");
            }
        }

        appHostLogs.EnsureNoErrors(cat => ShouldAssertInAppHostIgnoringResources(Path.GetFileNameWithoutExtension(appHostPath), cat));
        resourceLogs.EnsureNoErrors(ShouldAssertErrorsForResource);

        await app.StopAsync();
    }

    private static bool ShouldAssertInAppHostIgnoringResources(string appHostName, string categoryName)
        => !categoryName.StartsWith($"{appHostName}.Resources.", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldAssertErrorsForResource(IResource resource)
    {
        // Console.WriteLine($"ShouldAssertErrorsForResource: {resource.Name}: {resource.GetType()}");
        return resource
            is
                // Container resources tend to write to stderr for various reasons so only assert projects and executables
                (ProjectResource or ExecutableResource)
                // Node resources tend to have npm modules that write to stderr so ignore them
                and not NodeAppResource
            // Dapr resources write to stderr about deprecated --components-path flag
            && !resource.Name.EndsWith("-dapr-cli")
            && !resource.Name.StartsWith("pg");
    }

    public static TheoryData<string> AppHostAssemblies()
    {
        var appHostAssemblies = GetSamplesAppHostAssemblyPaths();
        var theoryData = new TheoryData<string>();
        string? appHostNameFilter = Environment.GetEnvironmentVariable("TEST_PLAYGROUND_APPHOST_FILTER");
        foreach (var asm in appHostAssemblies)
        {
            if (!string.IsNullOrEmpty(appHostNameFilter) && !asm.Contains(appHostNameFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            theoryData.Add(Path.GetRelativePath(AppContext.BaseDirectory, asm));
        }

        if (!theoryData.Any() && !string.IsNullOrEmpty(appHostNameFilter))
        {
            throw new InvalidOperationException($"No app host assemblies found matching filter '{appHostNameFilter}'");
        }

        return theoryData;
    }

    public static TheoryData<TestEndpoints> TestEndpoints()
    {
        IEnumerable<TestEndpoints> candidates =
        [
            new TestEndpoints("MySqlDb.AppHost", new() {
                { "apiservice", ["/alive", "/health", "/catalog"] },
            }),
            new TestEndpoints("PostgresEndToEnd.Apphost", new() {
                { "api", ["/", "/alive", "/health"] },
            }),
            // new TestEndpoints("Mongo.AppHost", new() {
            //     { "api", ["/alive", "/health"] },
            // }),
        ];

        TheoryData<TestEndpoints> data = new();
        string? appHostNameFilter = Environment.GetEnvironmentVariable("TEST_PLAYGROUND_APPHOST_FILTER");
        foreach (var candidateTestEndpoint in candidates)
        {
            if (string.IsNullOrEmpty(appHostNameFilter) || candidateTestEndpoint.AppHost?.Contains(appHostNameFilter, StringComparison.OrdinalIgnoreCase) == true)
            {
                data.Add(candidateTestEndpoint);
            }
        }

        return data;
    }

    private static IEnumerable<string> GetSamplesAppHostAssemblyPaths()
    {
        // All the AppHost projects are referenced by this project so we can find them by looking for all their assemblies in the base directory
        return Directory.GetFiles(AppContext.BaseDirectory, "*.AppHost.dll")
            .Where(fileName => !fileName.EndsWith("Aspire.Hosting.AppHost.dll", StringComparison.OrdinalIgnoreCase));
    }
}

public class TestEndpoints : IXunitSerializable
{
    // Required for deserialization
    public TestEndpoints() { }

    public TestEndpoints(string appHost, Dictionary<string, List<string>> resourceEndpoints)
    {
        AppHost = appHost;
        ResourceEndpoints = resourceEndpoints;
    }

    public string? AppHost { get; set; }

    public List<ResourceWait>? WaitForResources { get; set; }

    public Dictionary<string, List<string>>? ResourceEndpoints { get; set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        AppHost = info.GetValue<string>(nameof(AppHost));
        WaitForResources = JsonSerializer.Deserialize<List<ResourceWait>>(info.GetValue<string>(nameof(WaitForResources)));
        ResourceEndpoints = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(info.GetValue<string>(nameof(ResourceEndpoints)));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(AppHost), AppHost);
        info.AddValue(nameof(WaitForResources), JsonSerializer.Serialize(WaitForResources));
        info.AddValue(nameof(ResourceEndpoints), JsonSerializer.Serialize(ResourceEndpoints));
    }

    public override string? ToString() => $"{AppHost} ({ResourceEndpoints?.Count ?? 0} resources)";

    public class ResourceWait(string resourceName, string targetState)
    {
        public string ResourceName { get; } = resourceName;

        public string TargetState { get; } = targetState;

        public void Deconstruct(out string resourceName, out string targetState)
        {
            resourceName = ResourceName;
            targetState = TargetState;
        }
    }
}
