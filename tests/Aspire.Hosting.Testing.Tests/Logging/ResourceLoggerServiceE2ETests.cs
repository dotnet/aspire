// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Projects;
using Xunit;

namespace Aspire.Hosting.Testing.Tests.Logging;

public class ResourceLoggerServiceE2ETests(ITestOutputHelper output)
{
    [Fact]
    [RequiresDocker]
    public async Task ResourceLoggerService_WatchesAsync_CancelsWhenResourceEnded_ShortCommand()
    {
        var app = await BuildAppAsync(args: [], builder => {
            var webapp = new TestingAppHost1_MyWebApp();
            builder.AddExecutable("list-sdks-executable", "dotnet", Path.GetDirectoryName(webapp.ProjectPath)!, "--list-sdks");
        });

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var rls = app.Services.GetRequiredService<ResourceLoggerService>();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var exec = model.Resources.FirstOrDefault(x => x.Name == "list-sdks-executable");

        var startApp = app.StartAsync();

        var logNumber = 0;
        await foreach (var logBatch in rls.WatchAsync(exec!).WithCancellation(cts.Token))
        {
            foreach (var log in logBatch)
            {
                var type = log.IsErrorMessage ? "Error" : "Info";
                output.WriteLine($"Received log: #{++logNumber} [{type}] {log.Content}");
            }
        }

        // when this finishes, it should not finish via cts Token
        // but DCP has to close WatchAsync
        Assert.False(cts.IsCancellationRequested);

        // await startApp;
    }

    [Fact]
    [RequiresDocker]
    public async Task ResourceLoggerService_WatchesAsync_CancelsWhenResourceEnded()
    {
        var app = await BuildAppAsync(args: [], builder => {
            var webapp = new TestingAppHost1_MyWebApp();
            builder.AddExecutable("ping-executable", "ping", Path.GetDirectoryName(webapp.ProjectPath)!, "google.com");
        });
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var rls = app.Services.GetRequiredService<ResourceLoggerService>();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var exec = model.Resources.FirstOrDefault(x => x.Name == "ping-executable");

        var startApp = app.StartAsync();

        var logNumber = 0;
        await foreach (var logBatch in rls.WatchAsync(exec!).WithCancellation(cts.Token))
        {
            foreach (var log in logBatch)
            {
                var type = log.IsErrorMessage ? "Error" : "Info";
                output.WriteLine($"Received log: #{++logNumber} [{type}] {log.Content}");
            }
        }

        // when this finishes, it should not finish via cts Token
        // but DCP has to close WatchAsync
        Assert.False(cts.IsCancellationRequested);

        // await startApp;
    }

    private async Task<DistributedApplication> BuildAppAsync(
        string[] args,
        Action<IDistributedApplicationBuilder> builderAction,
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? configureBuilder = null)
    {
        configureBuilder ??= (appOptions, _) => { };
        var builder = DistributedApplicationTestingBuilder.Create(args, configureBuilder, typeof(TestingAppHost1_AppHost).Assembly)
            .WithTestAndResourceLogging(output);

        builderAction(builder);

        return await builder.BuildAsync();
    }

}
