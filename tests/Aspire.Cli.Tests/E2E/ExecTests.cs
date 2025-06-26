// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.Hosting;
using Projects;
using Xunit;
using Aspire.Hosting.Backchannel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Tests.E2E;

public class ExecTests(ITestOutputHelper output)
{
    [Fact]
    [RequiresDocker]
    public async Task Exec_InitializeMigrations_ShouldCreateMigrationsInWebApp()
    {
        var myWebAppProjectMetadata = new TestingAppHost1_MyWebApp();
        // DeleteMigrations(myWebAppProjectMetadata);

        // RUN apphost
        //string[] args = [
        //    "--operation", "run", // RUN type
        //    "--project", myWebAppProjectMetadata.ProjectPath, // apphost 
        //    "--add-postgres", // arbitraty flags for apphost
        //];

        // DOTNET LIST-SDKS COMMAND
        //string[] args = [
        //    "--operation", "exec", // EXEC type
        //    "--project", myWebAppProjectMetadata.ProjectPath, // apphost 
        //    "--resource", "mywebapp1", // target resource
        //    "--command", "\"dotnet --list-sdks\"", // command packed into string
        //    "--add-postgres", // arbitraty flags for apphost
        //];

        // PING GOOGLE.COM COMMAND
        //string[] args = [
        //    "--operation", "exec", // EXEC type
        //    "--project", myWebAppProjectMetadata.ProjectPath, // apphost 
        //    "--resource", "mywebapp1", // target resource
        //    "--command", "\"ping google.com\"", // command packed into string
        //    "--add-postgres", // arbitraty flags for apphost
        //];

        // ADD MIGRATION
        //string[] args = [
        //    "--operation", "exec", // EXEC type
        //    "--project", myWebAppProjectMetadata.ProjectPath, // apphost 
        //    "--resource", "mywebapp1", // target resource
        //    "--command", "\"dotnet ef migrations add Init --msbuildprojectextensionspath D:\\code\\aspire\\artifacts\\obj\\TestingAppHost1.MyWebApp\"", // command packed into string
        //    "--add-postgres", // arbitraty flags for apphost
        //];

        // APPLY MIGRATION UPDATE ON DB
        string[] args = [
            "--operation", "exec", // EXEC type
            "--project", myWebAppProjectMetadata.ProjectPath, // apphost 
            "--resource", "mywebapp1", // target resource
            "--command", "\"dotnet ef database update\"", // command packed into string
            "--add-postgres", // arbitraty flags for apphost
        ];

        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (appOptions, _) =>
        {
        };

        Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

        var builder = DistributedApplicationTestingBuilder.Create(args, configureBuilder, typeof(TestingAppHost1_AppHost).Assembly)
            .WithTestAndResourceLogging(output);

        // dependant of the target resource
        var pgsql = builder
            .AddPostgres(
                name: "postgres1",
                port: 6000,
                userName: builder.AddParameter("pgsqluser"),
                password: builder.AddParameter("pgsqlpass", secret: true))
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 6000));
        var pgsqlDb = pgsql.AddDatabase("postgresDb");

        //var connStr = await pgsql.Resource.GetConnectionStringAsync();
        //output.WriteLine("PGSQL Connection string: " + connStr);

        // the target resource
        var project = builder
            .AddProject<TestingAppHost1_MyWebApp>("mywebapp1")
            .WithReference(pgsqlDb)
            .WaitFor(pgsqlDb);

        await using var app = await builder.BuildAsync();

        var appHostRpcTarget = app.Services.GetRequiredService<AppHostRpcTarget>();
        var outputStream = appHostRpcTarget.ExecAsync(CancellationToken.None);

        var startTask = app.StartAsync(CancellationToken.None);
        await foreach (var message in outputStream)
        {
            var logLevel = message.IsErrorMessage ? LogLevel.Error : LogLevel.Information;
            output.WriteLine($"Received output: #{message.LineNumber} [level={logLevel}] [type={message.Type}] {message.Text}");
        }

        await startTask;

        AssertMigrationsCreated(myWebAppProjectMetadata);
        // DeleteMigrations(myWebAppProjectMetadata);
    }

#pragma warning disable IDE0051 // Remove unused private members
    private static void DeleteMigrations(IProjectMetadata projectMetadata)
#pragma warning restore IDE0051 // Remove unused private members
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
