// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
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
    private static string DatabaseMigrationsAppHostProjectPath =>
        Path.Combine(DatabaseMigration_AppHost.ProjectPath, "DatabaseMigration.AppHost.csproj");

    [Fact]
    [RequiresDocker]
    public async Task Exec_NotFoundTargetResource_ShouldProduceLogs()
    {
        string[] args = [
            "--operation", "run",
            "--project", DatabaseMigrationsAppHostProjectPath,
            "--resource", "randomnonexistingresource",
            "--command", "\"dotnet --info\"",
            "--postgres"
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs,
            x => x.Text.Contains("Target resource randomnonexistingresource not found in the model resources")
                && x.IsErrorMessage == true);
    }

    [Fact]
    [RequiresDocker]
    public async Task Exec_DotnetInfo_ShouldProduceLogs()
    {
        string[] args = [
            "--operation", "run",
            "--project", DatabaseMigrationsAppHostProjectPath,
            "--resource", "api",
            "--command", "\"dotnet --info\"",
            "--postgres"
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs, x => x.Text.Contains(".NET SDKs installed"));
        Assert.Contains(logs, x => x.Text.Contains("Aspire exec exit code: 0")); // success
    }

    [Fact]
    [RequiresDocker]
    public async Task Exec_DotnetBuildFail_ShouldProduceLogs()
    {
        string[] args = [
            "--operation", "run",
            "--project", DatabaseMigrationsAppHostProjectPath,
            "--resource", "api",
                         // not existing csproj, but we dont care if that succeeds or not - we are expecting
                         // whatever log output from the command
            "--command", "\"dotnet build \"MyRandom.csproj\"\"",
            "--postgres"
        ];

        /* Expected output:
                dotnet build "MyRandom.csproj"
                MSBUILD : error MSB1009: Project file does not exist.
                Switch: MyRandom.csproj
        */

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs, x => x.Text.Contains("Project file does not exist"));
        Assert.Contains(logs, x => x.Text.Contains("Aspire exec exit code: 1")); // not success
    }

    [Fact]
    [RequiresDocker]
    public async Task Exec_NonExistingCommand_ShouldProduceLogs()
    {
        string[] args = [
            "--operation", "run",
            "--project", DatabaseMigrationsAppHostProjectPath,
            "--resource", "api",
                         // not existing command. Executable should fail without start basically
            "--command", "\"randombuildcommand doit\"",
            "--postgres"
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs, x => x.Text.Contains("Aspire exec failed to start"));
    }

    [Fact]
    [RequiresDocker]
    public async Task Exec_DotnetHelp_ShouldProduceLogs()
    {
        string[] args = [
            "--operation", "run",
            "--project", DatabaseMigrationsAppHostProjectPath,
            "--resource", "api",
            "--command", "\"dotnet --help\"",
            "--postgres"
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs, x => x.Text.Contains("Usage: dotnet [sdk-options] [command] [command-options] [arguments]"));
        Assert.Contains(logs, x => x.Text.Contains("Aspire exec exit code: 0")); // success
    }

    [Fact]
    [RequiresDocker]
    [QuarantinedTest("https://github.com/dotnet/aspire/issues/10138")]
    public async Task Exec_InitializeMigrations_ShouldCreateMigrationsInWebApp()
    {
        // note: should also install dotnet-ef tool locally\globally

        var migrationName = "AddVersion";

        var apiModelProjectDir = @$"{MSBuildUtils.GetRepoRoot()}\playground\DatabaseMigration\DatabaseMigration.ApiModel\DatabaseMigration.ApiModel.csproj";
        DeleteMigrations(apiModelProjectDir, migrationName);

        string[] args = [
            "--operation", "run",
            "--project", DatabaseMigrationsAppHostProjectPath,
            "--resource", "api",
            "--command", $"\"dotnet ef migrations add AddVersion --project {apiModelProjectDir}\"",
            "--postgres"
        ];

        var app = await BuildAppAsync(args);
        var logs = await ExecAndCollectLogsAsync(app, timeoutSec: 60);

        Assert.True(logs.Count > 0, "No logs were produced during the exec operation.");
        Assert.Contains(logs, x => x.Text.Contains("Build started"));
        Assert.Contains(logs, x => x.Text.Contains("Build succeeded"));

        AssertMigrationsCreated(apiModelProjectDir, migrationName);
        DeleteMigrations(apiModelProjectDir, migrationName);
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

        IResourceBuilder<IResourceWithConnectionString> database;
        if (args.Contains("--postgres"))
        {
            database = builder.AddPostgres("sql1").AddDatabase("db1");
        }
        else
        {
            database = builder.AddSqlServer("sql1").AddDatabase("db1");
        }

        var project = builder
            .AddProject<DatabaseMigration_ApiService>("api")
            .WithReference(database)
            .WaitFor(database);

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
