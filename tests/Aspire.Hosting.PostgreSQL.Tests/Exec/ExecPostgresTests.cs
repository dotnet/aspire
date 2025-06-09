// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Hosting;
using Projects;
using Xunit;

namespace Aspire.Hosting.PostgreSQL.Tests.Exec;

public class ExecPostgresTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Postgre()
    {
        // if you run aspire.cli against some random appHost csproj,
        // it will end up running something like:
        // 'run --no-build  --project C:\code\aspire\tests\TestingAppHost1\TestingAppHost1.AppHost\TestingAppHost1.AppHost.csproj --'

        string[] args = [ " " ];
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings> configureBuilder = (_, _) => { };

        var builder = DistributedApplicationTestingBuilder.Create(args, configureBuilder)
            .WithTestAndResourceLogging(output);

        var postgres = builder
            .AddPostgres("postgres")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        var project = builder
            .AddProject<TestingAppHost1_MyWebApp>("mywebapp1")
            .WithReference(postgres);

        await using var app = await builder.BuildAsync();
        await app.StartAsync();

        // Wait for the application to be ready
        await app.WaitForTextAsync("Application started.").WaitAsync(TimeSpan.FromMinutes(1));
    }
}
