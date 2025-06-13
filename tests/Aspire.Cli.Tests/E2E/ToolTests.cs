// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Utils;
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
            "--project", @"C:\code\aspire\tests\TestingAppHost1\TestingAppHost1.AppHost\TestingAppHost1.AppHost.csproj",
            // what resource to target
            "--resource", "mywebapp1",
            // command to execute against resource
            // note: there is an issue with dotnet-ef when artifacts are not in the local obj, so you have to specify the obj\ location
            // https://github.com/dotnet/efcore/issues/23853#issuecomment-2183607932
            "--command", "dotnet ef migrations add Init --msbuildprojectextensionspath C:\\code\\aspire\\artifacts\\obj\\TestingAppHost1.MyWebApp"
        ];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (appOptions, _) =>
        {
        };

        var builder = DistributedApplicationTestingBuilder.Create(args, configureBuilder, typeof(TestingAppHost1_AppHost).Assembly)
            .WithTestAndResourceLogging(output);

        // independent resource (should not be running)
        //var miniPostgres = builder
        //    .AddPostgres("miniPostgres")
        //    .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));
        //miniPostgres.AddDatabase("miniDb");

        // dependant of the target resource
        var postgres = builder
            .AddPostgres("postgres")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));
        var mainDb = postgres.AddDatabase("mainDb");

        // the target resource
        var project = builder
            .AddProject<TestingAppHost1_MyWebApp>("mywebapp1")
            .WithReference(postgres);

        // independent resource (should not be running)
        // builder.AddProject<TestingAppHost1_MyWorker>("myworker");

        builder
            .AddExecutable("migrate", "dotnet", new TestingAppHost1_MyWebApp().ProjectPath, "ef migrations add Init")
            .WaitFor(project)
            .AsTool();

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        var exec = app.Services.GetRequiredService<IDcpExecutor>();

        AssertMigrationsCreated(myWebAppProjectMetadata);
        await app.WaitForTextAsync("Application started.").WaitAsync(TimeSpan.FromMinutes(1));
    }

    private static void DeleteMigrations(IProjectMetadata projectMetadata)
    {
        var projectDirectory = Path.GetDirectoryName(projectMetadata.ProjectPath);
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
