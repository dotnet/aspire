// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tools;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Projects;
using Xunit;

namespace Aspire.Cli.Tests.E2E;

public class ToolTests(ITestOutputHelper output)
{
    [Fact]
    [RequiresDocker]
    public async Task Exec_InitializeMigrations_ShouldCreateMigrationsInWebApp()
    {
        var myWebAppProjectMetadata = new TestingAppHost1_MyWebApp();
        DeleteMigrations(myWebAppProjectMetadata);

        string[] args = [
            // separate type of command
            "--operation", "tool",
            // what AppHost to target
            "--project", myWebAppProjectMetadata.ProjectPath,
            // what resource to target
            "--tool", "migration-add",

            // if there are other args - it will break because EF does not process extra non-mapped args correctly
            // "--add-postgres"
        ];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (appOptions, _) =>
        {
        };

        var builder = DistributedApplicationTestingBuilder.Create(args, configureBuilder, typeof(TestingAppHost1_AppHost).Assembly)
            .WithTestAndResourceLogging(output);

        // dependant of the target resource
        var postgres = builder
            .AddPostgres("postgres")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));
        var postresDb = postgres.AddDatabase("postgresDb");

        // the target resource
        var project = builder
            .AddProject<TestingAppHost1_MyWebApp>("mywebapp1")
            .WithReference(postgres);

        builder
            .AddExecutable(
                name: "migration-add",
                command: "dotnet",
                workingDirectory: (new TestingAppHost1_MyWebApp()).ProjectPath,
                args: "ef migrations add Init")
            // note: there is an issue with dotnet-ef when artifacts are not in the local obj, so you have to specify the obj\ location
            // https://github.com/dotnet/efcore/issues/23853#issuecomment-2183607932;
            .WithArgs([ "--msbuildprojectextensionspath", "../../../artifacts/obj/TestingAppHost1.MyWebApp" ])
            .WithExplicitStart();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // in real world this would be invoked via cli, but we can resolve service for simplicity
        var toolExecutionService = app.Services.GetRequiredService<ToolExecutionService>();
        var commandOutput = toolExecutionService.ExecuteToolAndStreamOutputAsync(CancellationToken.None);
        await foreach (var command in commandOutput)
        {
            output.WriteLine($"Tool execution output: [iserror={command.IsError}] {command.Text}");
        }

        AssertMigrationsCreated(myWebAppProjectMetadata);
        DeleteMigrations(myWebAppProjectMetadata);
    }

    private static void DeleteMigrations(IProjectMetadata projectMetadata)
    {
        var projectDirectory = Path.GetDirectoryName(projectMetadata.ProjectPath);
        if (!Directory.Exists(projectDirectory))
        {
            return;
        }

        var migrationFiles = Directory.GetFiles(Path.Combine(projectDirectory!, "Migrations"));

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
