// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests;
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
    public async Task Exec_DotnetInfo_ShouldProduceLogs()
    {
        var apiService = new DatabaseMigration_ApiService();

        string[] args = [
            "--operation", "exec",
            "--project", apiService.ProjectPath,
            "--resource", "api",
            "--command", "\"dotnet --info\""
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs, x => x.Contains(".NET SDKs installed"));
    }

    [Fact]
    [RequiresDocker]
    public async Task Exec_DotnetHelp_ShouldProduceLogs()
    {
        var apiService = new DatabaseMigration_ApiService();

        string[] args = [
            "--operation", "exec",
            "--project", apiService.ProjectPath,
            "--resource", "api",
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
        var migrationName = "AddVersion";

        var apiModelProjectDir = @$"{MSBuildUtils.GetRepoRoot()}\playground\DatabaseMigration\DatabaseMigration.ApiModel";
        DeleteMigrations(apiModelProjectDir, migrationName);

        string[] args = [
            "--operation", "exec",
            "--project", Path.Combine(DatabaseMigration_AppHost.ProjectPath, "DatabaseMigration.AppHost.csproj"),
            "--resource", "api",
            "--command", $"\"dotnet ef migrations add AddVersion --project {apiModelProjectDir}\""
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app, timeoutSec: 60);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs, x => x.Contains("Build started"));
        Assert.Contains(logs, x => x.Contains("Build succeeded"));

        AssertMigrationsCreated(apiModelProjectDir, migrationName);
        DeleteMigrations(apiModelProjectDir, migrationName);
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
        var builder = DistributedApplicationTestingBuilder.Create(args, configureBuilder, typeof(DatabaseMigration_AppHost).Assembly)
            .WithTestAndResourceLogging(output);

        var sqlServerDb = builder.AddSqlServer("sql1").AddDatabase("db1");

        var project = builder
            .AddProject<DatabaseMigration_ApiService>("api")
            .WithReference(sqlServerDb)
            .WaitFor(sqlServerDb);

        return await builder.BuildAsync();
    }

    private static void DeleteMigrations(string projectDirectory, params string[] fileExpectedNames)
    {
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
                if (fileExpectedNames.Any(migrationFile.Contains))
                {
                    File.Delete(migrationFile);
                }
            }
            catch (FileNotFoundException)
            {
                // ignore if not exists
            }
        }
    }

    private void AssertMigrationsCreated(
        string projectDirectory,
        params string[] expectedFileNames)
    {
        var migrationFiles = Directory.GetFiles(Path.Combine((string)projectDirectory!, "Migrations"));
        Assert.NotEmpty(migrationFiles);

        var createdMigrationFiles = new List<string>();
        foreach (var file in migrationFiles)
        {
            if (expectedFileNames.Any(file.Contains))
            {
                createdMigrationFiles.Add(file);
                output.WriteLine("ASSERT: Created migration file found: " + file);
            }
        }

        // At least one migration should be found with expected file names
        Assert.NotEmpty(createdMigrationFiles);
    }
}
