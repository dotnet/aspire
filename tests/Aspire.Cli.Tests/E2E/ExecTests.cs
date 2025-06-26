// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
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

public class ExecTests(ITestOutputHelper output)
{
    [Fact]
    [RequiresDocker]
    public async Task Exec_PingGoogleCom_ShouldProduceLogs()
    {
        var myWebAppProjectMetadata = new TestingAppHost1_MyWebApp();

        string[] args = [
            "--operation", "exec",
            "--project", myWebAppProjectMetadata.ProjectPath,
            "--resource", "mywebapp1",
            "--command", "\"ping google.com\""
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs, x => x.Contains("Reply from"));
        Assert.Contains(logs, x => x.Contains("Ping statistics for"));
        Assert.Contains(logs, x => x.Contains("Approximate round trip times in milli-seconds"));
    }

    [Fact]
    [RequiresDocker]
    public async Task Exec_DotnetHelp_ShouldProduceLogs()
    {
        var myWebAppProjectMetadata = new TestingAppHost1_MyWebApp();

        string[] args = [
            "--operation", "exec",
            "--project", myWebAppProjectMetadata.ProjectPath,
            "--resource", "mywebapp1",
            "--command", "\"dotnet --help\""
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs, x => x.Contains("Usage: dotnet [sdk-options] [command] [command-options] [arguments]"));
    }

    [Fact]
    [RequiresDocker]
    public async Task Exec_InitializeMigrations_ShouldCreateMigrationsInWebApp()
    {
        var myWebAppProjectMetadata = new TestingAppHost1_MyWebApp();
        DeleteMigrations(myWebAppProjectMetadata);

        string[] args = [
            "--operation", "exec",
            "--project", myWebAppProjectMetadata.ProjectPath,
            "--resource", "mywebapp1",
            "--command", "\"dotnet ef migrations add Init --msbuildprojectextensionspath D:\\code\\aspire\\artifacts\\obj\\TestingAppHost1.MyWebApp\"",
            "--add-postgres",
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs, x => x.Contains("Build started"));

        AssertMigrationsCreated(myWebAppProjectMetadata);
        DeleteMigrations(myWebAppProjectMetadata);
    }

    private async Task<List<string>> ExecAndCollectLogsAsync(DistributedApplication app, int timeoutSec = 30)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));

        var appHostRpcTarget = app.Services.GetRequiredService<AppHostRpcTarget>();
        var outputStream = appHostRpcTarget.ExecAsync(cts.Token);

        var logs = new List<string>();
        var startTask = app.StartAsync(cts.Token);
        await foreach (var message in outputStream)
        {
            var logLevel = message.IsErrorMessage ? LogLevel.Error : LogLevel.Information;
            var log = $"Received output: #{message.LineNumber} [level={logLevel}] [type={message.Type}] {message.Text}";

            logs.Add(log);
            output.WriteLine(log);
        }

        await startTask;
        return logs;
    }

    private async Task<DistributedApplication> BuildAppAsync(string[] args, Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? configureBuilder = null)
    {
        configureBuilder ??= (appOptions, _) => { };
        var builder = DistributedApplicationTestingBuilder.Create(args, configureBuilder, typeof(TestingAppHost1_AppHost).Assembly)
            .WithTestAndResourceLogging(output);

        var pgsqlDb = builder
            .AddPostgres("postgres1", port: 6000)
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 6000))
            .AddDatabase("postgresDb");

        var project = builder
            .AddProject<TestingAppHost1_MyWebApp>("mywebapp1")
            .WithReference(pgsqlDb)
            .WaitFor(pgsqlDb);

        return await builder.BuildAsync();
    }

    private static void DeleteMigrations(IProjectMetadata projectMetadata)
    {
        var projectDirectory = Path.GetDirectoryName(projectMetadata.ProjectPath);
        if (!Directory.Exists(projectDirectory))
        {
            return;
        }

        var migrationDirectory = Path.Combine(projectDirectory!, "Migrations");
        if (!Directory.Exists(migrationDirectory))
        {
            return;
        }

        var migrationFiles = Directory.GetFiles(migrationDirectory);
        foreach (var migrationFile in migrationFiles)
        {
            try
            {
                File.Delete(migrationFile);
            }
            catch (FileNotFoundException)
            {
                // ignore if not exists
            }
        }
    }

    private static void AssertMigrationsCreated(IProjectMetadata projectMetadata)
    {
        var projectDirectory = Path.GetDirectoryName(projectMetadata.ProjectPath);
        var migrationFiles = Directory.GetFiles(Path.Combine(projectDirectory!, "Migrations"));
        Assert.NotEmpty(migrationFiles);
        Assert.All(migrationFiles, file =>
            Assert.True(file.Contains("Init", StringComparison.OrdinalIgnoreCase)
                     || file.Contains("Snapshot", StringComparison.OrdinalIgnoreCase))
        );
    }
}
