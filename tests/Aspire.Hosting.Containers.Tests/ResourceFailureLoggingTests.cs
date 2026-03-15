// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using TestConstants = Microsoft.AspNetCore.InternalTesting.TestConstants;

namespace Aspire.Hosting.Containers.Tests;

[Trait("Partition", "6")]
public class ContainerResourceFailureLoggingTests(ITestOutputHelper testOutputHelper)
{
    /*
    [Fact]
    [RequiresFeature(TestFeature.Docker)]
    // https://github.com/dotnet/aspire/issues/13756
    public async Task IllegalBindMount()
    {
        using var cts = DefaultCancellationTokenSource();
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var illegalPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "blah:\\invalid"
            : "/dev/null/invalid";

        var container = builder.AddContainer("container", "nginx")
            .WithBindMount(illegalPath, "/mtn/whatever");
        AddFakeLogging(container);

        FakeLogCollector logCollector;
        using (var app = builder.Build())
        {
            logCollector = app.Services.GetFakeLogCollector();
            await app.StartAsync(cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
            await app.ResourceNotifications.WaitForResourceAsync(container.Resource.Name, KnownResourceStates.FailedToStart, cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
        }

        var logLines = GetLogLines(logCollector);
        Assert.Contains(logLines, x =>
            x.Contains("Error response from daemon") ||
            x.Contains("Could not verify existence of bind mount source path"));
    }
    */

    [Fact]
    [RequiresFeature(TestFeature.Docker)]
    public async Task BadContainerRuntimeArg()
    {
        using var cts = DefaultCancellationTokenSource();
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var container = builder.AddContainer("container", "nginx")
            .WithContainerRuntimeArgs("--illegal");
        AddFakeLogging(container);

        FakeLogCollector logCollector;
        using (var app = builder.Build())
        {
            logCollector = app.Services.GetFakeLogCollector();
            await app.StartAsync(cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
            await app.ResourceNotifications.WaitForResourceAsync(container.Resource.Name, KnownResourceStates.FailedToStart, cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
        }

        var logLines = GetLogLines(logCollector);
        Assert.Contains(logLines, x => x.EndsWith("unknown flag: --illegal"));
    }

    [Fact]
    [RequiresFeature(TestFeature.Docker)]
    public async Task BadImage()
    {
        using var cts = DefaultCancellationTokenSource();
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var container = builder.AddContainer("container", "does-not-exist")
            .WithImageRegistry("does.not.exist.internal")
            .WithImagePullPolicy(ImagePullPolicy.Always);
        AddFakeLogging(container);

        FakeLogCollector logCollector;
        using (var app = builder.Build())
        {
            logCollector = app.Services.GetFakeLogCollector();
            await app.StartAsync(cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
            await app.ResourceNotifications.WaitForResourceAsync(container.Resource.Name, KnownResourceStates.FailedToStart, cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
        }

        var logLines = GetLogLines(logCollector);
        Assert.Contains(logLines, x => x.Contains("Error response from daemon"));
    }

    [Fact]
    [RequiresFeature(TestFeature.Docker)]
    public async Task NeedsAuthentication()
    {
        using var cts = DefaultCancellationTokenSource();
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var container = builder.AddContainer("container", "mattermost.com/go-msft-fips:1.24.6")
            .WithImageRegistry("cgr.dev")
            .WithImagePullPolicy(ImagePullPolicy.Always);
        AddFakeLogging(container);

        FakeLogCollector logCollector;
        using (var app = builder.Build())
        {
            logCollector = app.Services.GetFakeLogCollector();
            await app.StartAsync(cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
            await app.ResourceNotifications.WaitForResourceAsync(container.Resource.Name, KnownResourceStates.FailedToStart, cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
        }

        var logLines = GetLogLines(logCollector);
        Assert.Contains(logLines, x => x.EndsWith("Error response from daemon: error from registry: Authentication required"));
    }

    /*
    [Fact]
    [RequiresFeature(TestFeature.Docker)]
    // https://github.com/dotnet/aspire/issues/14262 
    public async Task ContainerExitsImmediatelyAfterStart()
    {
        using var cts = DefaultCancellationTokenSource();
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var container = builder.AddContainer("container", "alpine")
            .WithEntrypoint("sh")
            .WithArgs("-c", """
                echo "Hello from Stdout"
                >&2 echo "Hello from Stderr"
                sleep 1
                exit 123
                """);
        AddFakeLogging(container);

        FakeLogCollector logCollector;
        using (var app = builder.Build())
        {
            logCollector = app.Services.GetFakeLogCollector();
            await app.StartAsync(cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
            // This ought to be `Exited` instead - https://github.com/dotnet/aspire/issues/13760
            await app.ResourceNotifications.WaitForResourceAsync(container.Resource.Name, KnownResourceStates.FailedToStart, cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
            await app.StopAsync(cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
        }

        var logLines = GetLogLines(logCollector);
        Assert.Contains(logLines, x => x.EndsWith("Hello from Stdout") || x.EndsWith("Hello from Stderr"));
    }
    */
    private static CancellationTokenSource DefaultCancellationTokenSource()
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMinutes(1));
        return cts;
    }

    private static void AddFakeLogging<T>(IResourceBuilder<T> builder)
        where T : IResource
    {
        var category = $"{builder.ApplicationBuilder.Environment.ApplicationName}.Resources.{builder.Resource.Name}";
        builder.ApplicationBuilder.Services.AddLogging(x => x.AddFakeLogging(y => y.FilteredCategories.Add(category)));
    }

    private static List<string> GetLogLines(FakeLogCollector logCollector)
    {
        return [.. logCollector.GetSnapshot()
                .Select(x => x.StructuredState?.SingleOrDefault(x => x.Key == "LineContent"))
                .Where(x => x is not null)
                .Select(x => x?.Value!)];
    }
}
