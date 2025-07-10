// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Projects;
using Xunit;

namespace Aspire.Cli.Tests.E2E;

public class ExecContainerResourceTests(ITestOutputHelper output)
{
    private static string ContainersAppHostProjectPath =>
        Path.Combine(Containers_AppHost.ProjectPath, "Containers.AppHost.csproj");

    [Fact]
    [RequiresDocker]
    public async Task Exec_ListFilesInDirectory_ShouldProduceLogs()
    {
        string[] args = [
            "--operation", "run",
            "--project", ContainersAppHostProjectPath,
            "--resource", "nginx",
            "--command", "\"ls\""
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app, timeoutSec: /* TODO remove after debugging */ 6000);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
    }

    private async Task<List<CommandOutput>> ExecAndCollectLogsAsync(DistributedApplication app, int timeoutSec = 30)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));

        var appHostRpcTarget = app.Services.GetRequiredService<AppHostRpcTarget>();
        var outputStream = appHostRpcTarget.ExecAsync(cts.Token);

        var logs = new List<CommandOutput>();
        var startTask = app.StartAsync(cts.Token);
        await foreach (var message in outputStream)
        {
            var logLevel = message.IsErrorMessage ? LogLevel.Error : LogLevel.Information;
            var log = $"Received output: #{message.LineNumber} [level={logLevel}] [type={message.Type}] {message.Text}";

            logs.Add(message);
            output.WriteLine(log);
        }

        await startTask;
        return logs;
    }

    private async Task<DistributedApplication> BuildAppAsync(string[] args, Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? configureBuilder = null)
    {
        configureBuilder ??= (appOptions, _) => { };
        var builder = DistributedApplicationTestingBuilder.Create(args, configureBuilder, typeof(DatabaseMigration_AppHost).Assembly)
            .WithTestAndResourceLogging(output);

        var apiService = builder.AddProject<Containers_ApiService>("apiservice");
        var nginx = builder.AddContainer("nginx", "nginx", "1.25");

        return await builder.BuildAsync();
    }
}
