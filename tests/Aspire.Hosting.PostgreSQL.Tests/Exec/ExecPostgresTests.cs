// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.Hosting;
using Projects;
using Xunit;

namespace Aspire.Hosting.PostgreSQL.Tests.Exec;

public class ExecPostgresTests(ITestOutputHelper output)
{
    [Fact]
    [RequiresDocker]
    public async Task Postgre()
    {
        string[] args = [
            // separate type of command 'exec'
            "--operation", "exec",
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

        Environment.SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME", "podman");

        var builder = DistributedApplicationTestingBuilder.Create(args, configureBuilder, typeof(TestingAppHost1_AppHost).Assembly)
            .WithTestAndResourceLogging(output);

        var miniPostgres = builder
            .AddPostgres("miniPostgres")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));
        miniPostgres.AddDatabase("miniDb");

        var postgres = builder
            .AddPostgres("postgres")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));
        postgres.AddDatabase("mainDb");

        var project = builder
            .AddProject<TestingAppHost1_MyWebApp>("mywebapp1")
            .WithReference(postgres);

        // independent resource
        builder
            .AddProject<TestingAppHost1_MyWorker>("myworker");

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Wait for the application to be ready
        await app.WaitForTextAsync("Application started.").WaitAsync(TimeSpan.FromMinutes(1));
    }
}
