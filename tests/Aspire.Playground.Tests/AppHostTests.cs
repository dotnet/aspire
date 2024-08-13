// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Polly.Timeout;
using SamplesIntegrationTests;
using SamplesIntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Aspire.Playground.Tests;

public class AppHostTests
{
    private readonly ITestOutputHelper _testOutput;
    private static readonly string? s_appHostNameFilter = Environment.GetEnvironmentVariable("TEST_PLAYGROUND_APPHOST_FILTER");

    public AppHostTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Theory]
    [MemberData(nameof(AppHostAssemblies))]
    public async Task AppHostRunsCleanly(string appHostPath)
    {
        var appHost = await DistributedApplicationTestFactory.CreateAsync(appHostPath, _testOutput);
        await using var app = await appHost.BuildAsync();

        await app.StartAsync();
        await app.WaitForResources().WaitAsync(TimeSpan.FromMinutes(2));

        app.EnsureNoErrorsLogged();

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

        await app.StartAsync();

        if (testEndpoints.WaitForTexts != null)
        {
            // If specific ready to start texts are available use it
            var tasks = testEndpoints.WaitForTexts.Select(x => app.WaitForTextAsync(log => new Regex(x.Pattern).IsMatch(log), x.ResourceName));
            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromMinutes(5));
        }
        else
        {
            var applicationModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();

            await app.WaitForResources().WaitAsync(TimeSpan.FromMinutes(5));

            if (testEndpoints.WaitForResources?.Count > 0)
            {
                // Wait until each resource transitions to the required state
                var timeout = TimeSpan.FromMinutes(5);
                foreach (var (ResourceName, TargetState) in testEndpoints.WaitForResources)
                {
                    _testOutput.WriteLine($"Waiting for resource '{ResourceName}' to reach state '{TargetState}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}'");
                    await app.WaitForResource(ResourceName, TargetState).WaitAsync(TimeSpan.FromMinutes(5));
                }
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
                        resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
                        resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                        resilience.Retry.MaxRetryAttempts = 30;
                        resilience.CircuitBreaker.SamplingDuration = resilience.AttemptTimeout.Timeout * 2;
                    });
            });

            foreach (var path in endpoints)
            {
                _testOutput.WriteLine($"Calling endpoint '{client.BaseAddress}{path.TrimStart('/')} for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}'");
                try
                {
                    response = await client.GetAsync(path);
                }
                catch (TimeoutRejectedException tre)
                {
                    throw new XunitException($"Endpoint '{client.BaseAddress}{path.TrimStart('/')}' for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}' timed out", tre);
                }

                Assert.True(HttpStatusCode.OK == response.StatusCode, $"Endpoint '{client.BaseAddress}{path.TrimStart('/')}' for resource '{resource}' in app '{Path.GetFileNameWithoutExtension(appHostPath)}' returned status code {response.StatusCode}");
            }
        }

        app.EnsureNoErrorsLogged();
        await app.StopAsync();
    }

    public static TheoryData<string> AppHostAssemblies()
    {
        var appHostAssemblies = GetPlaygroundAppHostAssemblyPaths();
        var theoryData = new TheoryData<string>();
        foreach (var asm in appHostAssemblies)
        {
            if (string.IsNullOrEmpty(s_appHostNameFilter) || asm.Contains(s_appHostNameFilter, StringComparison.OrdinalIgnoreCase))
            {
                theoryData.Add(Path.GetRelativePath(AppContext.BaseDirectory, asm));
            }
        }

        if (!theoryData.Any() && !string.IsNullOrEmpty(s_appHostNameFilter))
        {
            throw new InvalidOperationException($"No app host assemblies found matching filter '{s_appHostNameFilter}'");
        }

        return theoryData;
    }

    public static TheoryData<TestEndpoints> TestEndpoints()
    {
        IList<TestEndpoints> candidates =
        [
            new TestEndpoints("CosmosEndToEnd.AppHost",
                resourceEndpoints: new() { { "api", ["/alive", "/health", "/", "/ef"] } },
                waitForTexts: [
                    new ("cosmos", "Started$"),
                    new ("api", "Application started")
                ]),
            // Issue: https://github.com/dotnet/aspire/issues/5274
            //new TestEndpoints("Mongo.AppHost",
                //resourceEndpoints: new() { { "api", ["/alive", "/health", "/"] } },
                //waitForTexts: [
                    //new ("mongo", "Waiting for connections"),
                    //new ("mongo-mongoexpress", "Mongo Express server listening"),
                    //new("api", "Application started.")
                //]),
            new TestEndpoints("MySqlDb.AppHost",
                resourceEndpoints: new() { { "apiservice", ["/alive", "/health", "/catalog"] } },
                waitForTexts: [
                    new ("mysql", "ready for connections.* port: 33060"),
                    new ("apiservice", "Application started")
                ]),
            new TestEndpoints("Nats.AppHost",
                resourceEndpoints: new() {
                    { "api", ["/alive", "/health"] },
                    { "backend", ["/alive", "/health"] }
                },
                waitForTexts: [
                    new ("nats", "Server is ready"),
                    new("api", "Application started")
                ]),
            new TestEndpoints("ParameterEndToEnd.AppHost",
                resourceEndpoints: new() { { "api", ["/", "/alive", "/health"] } },
                waitForTexts: [
                    new ("sql", "SQL Server is now ready for client connections."),
                ]),
            new TestEndpoints("PostgresEndToEnd.AppHost",
                resourceEndpoints: new() {
                    // Invoking "/" first as that *creates* the databases
                    { "api", ["/", "/alive", "/health"] }
                },
                waitForTexts: [
                    new ("pg1", "PostgreSQL init process complete; ready for start up"),
                    new ("pg2", "PostgreSQL init process complete; ready for start up"),
                    new ("pg3", "PostgreSQL init process complete; ready for start up"),
                    new ("pg4", "PostgreSQL init process complete; ready for start up"),
                    new ("pg5", "PostgreSQL init process complete; ready for start up"),
                    new ("pg6", "PostgreSQL init process complete; ready for start up"),
                    new ("pg10", "PostgreSQL init process complete; ready for start up"),
                ]),
            new TestEndpoints("ProxylessEndToEnd.AppHost",
                resourceEndpoints: new() { { "api", ["/alive", "/health", "/redis"] } },
                waitForTexts: [
                    new ("redis", "Ready to accept connections"),
                    new("api", "Application started"),
                    new("api2", "Application started")
                ]),
            new TestEndpoints("Qdrant.AppHost",
                resourceEndpoints: new() { { "apiservice", ["/alive", "/health"] } },
                waitForTexts: [
                    new ("qdrant", "Qdrant HTTP listening"),
                    new ("apiservice", "Application started")
                ]),
            new TestEndpoints("Seq.AppHost",
                resourceEndpoints: new() { { "api", ["/alive", "/health"] } },
                waitForTexts: [
                    new ("seq", "Seq listening"),
                    new ("api", "Application started")
                ]),
            // Invoke "/" first to create the databases
            new TestEndpoints("SqlServerEndToEnd.AppHost",
                resourceEndpoints: new() { { "api", ["/", "/alive", "/health"] } },
                waitForTexts: [
                    new ("sql1", "SQL Server is now ready for client connections"),
                    new ("sql2", "SQL Server is now ready for client connections"),
                ]),
            new TestEndpoints("TestShop.AppHost",
                resourceEndpoints: new() {
                    { "catalogdbapp", ["/alive", "/health"] },
                    { "frontend", ["/alive", "/health"] }
                },
                waitForTexts: [
                    new ("messaging", "started TCP listener"),
                    new ("basketcache", "Ready to accept connections"),
                    new ("frontend", "Application started"),
                    new ("catalogdbapp", "Application started"),
                    new ("basketservice", "Application started"),
                    new ("postgres", "database system is ready to accept connections"),
                ])
        ];

        TheoryData<TestEndpoints> theoryData = new();
        foreach (var candidateTestEndpoint in candidates)
        {
            if (string.IsNullOrEmpty(s_appHostNameFilter) || candidateTestEndpoint.AppHost?.Contains(s_appHostNameFilter, StringComparison.OrdinalIgnoreCase) == true)
            {
                theoryData.Add(candidateTestEndpoint);
            }
        }

        if (!theoryData.Any() && !string.IsNullOrEmpty(s_appHostNameFilter))
        {
            throw new InvalidOperationException($"No test endpoints found matching filter '{s_appHostNameFilter}'");
        }

        return theoryData;
    }

    private static IEnumerable<string> GetPlaygroundAppHostAssemblyPaths()
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

    public TestEndpoints(string appHost, Dictionary<string, List<string>> resourceEndpoints, List<ReadyStateText>? waitForTexts = null)
    {
        AppHost = appHost;
        ResourceEndpoints = resourceEndpoints;
        WaitForTexts = waitForTexts;
    }

    public string? AppHost { get; set; }

    public List<ResourceWait>? WaitForResources { get; set; }

    public List<ReadyStateText>? WaitForTexts { get; set; }

    public Dictionary<string, List<string>>? ResourceEndpoints { get; set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        AppHost = info.GetValue<string>(nameof(AppHost));
        WaitForResources = JsonSerializer.Deserialize<List<ResourceWait>>(info.GetValue<string>(nameof(WaitForResources)));
        WaitForTexts = JsonSerializer.Deserialize<List<ReadyStateText>>(info.GetValue<string>(nameof(WaitForTexts)));
        ResourceEndpoints = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(info.GetValue<string>(nameof(ResourceEndpoints)));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(AppHost), AppHost);
        info.AddValue(nameof(WaitForResources), JsonSerializer.Serialize(WaitForResources));
        info.AddValue(nameof(WaitForTexts), JsonSerializer.Serialize(WaitForTexts));
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

    public record class ReadyStateText(string ResourceName, string Pattern);
}
