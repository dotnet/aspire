// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using TestConstants = Microsoft.AspNetCore.InternalTesting.TestConstants;

namespace Aspire.Hosting.Tests;

[Trait("Partition", "2")]
public class ExecutableResourceFailureLoggingTests(ITestOutputHelper testOutputHelper)
{
    /*
    // https://github.com/dotnet/aspire/issues/10218#issuecomment-3712609775 
    [Fact]
    public async Task ExecutableDoesNotExist()
    {
        using var cts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.DefaultOrchestratorTestLongTimeout);
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var container = builder.AddExecutable("exe", "does-not-exist", "");
        AddFakeLogging(container);

        FakeLogCollector logCollector;
        using (var app = builder.Build())
        {
            logCollector = app.Services.GetFakeLogCollector();
            await app.StartAsync(cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
            await app.ResourceNotifications.WaitForResourceAsync(container.Resource.Name, KnownResourceStates.FailedToStart, cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
        }

        var logLines = GetLogLines(logCollector);

        var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "%PATH%" : "$PATH";
        Assert.Contains(logLines, x => x.EndsWith($"[sys] Failed to start a process: Cmd = does-not-exist, Args = [], Error = exec: \"does-not-exist\": executable file not found in {path}"));
        Assert.Contains(logLines, x => x.EndsWith($"[sys] An attempt to start the Executable failed: Error = exec: \"does-not-exist\": executable file not found in {path}"));
    }
    */

    [Fact]
    public async Task ExecutableExitsImmediately()
    {
        using var cts = AsyncTestHelpers.CreateDefaultTimeoutTokenSource(TestConstants.DefaultOrchestratorTestLongTimeout);
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var executable = builder.AddExecutable("pwsh", "pwsh", "")
            .WithArgs("-Command", """
                Write-Host "Hello from Stdout"
                [Console]::Error.WriteLine("Hello from Stderr")
                """);
        AddFakeLogging(executable);

        FakeLogCollector logCollector;
        using (var app = builder.Build())
        {
            logCollector = app.Services.GetFakeLogCollector();
            await app.StartAsync(cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
            await app.ResourceNotifications.WaitForResourceAsync(executable.Resource.Name, KnownResourceStates.Finished, cts.Token).DefaultTimeout(TestConstants.DefaultOrchestratorTestLongTimeout);
        }

        var logLines = GetLogLines(logCollector);

        Assert.Contains(logLines, x => x.EndsWith("Hello from Stdout"));
        Assert.Contains(logLines, x => x.EndsWith("Hello from Stderr"));
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
