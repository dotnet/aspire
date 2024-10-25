// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddAzurePostgresFlexibleServer("pg")
                .WithPasswordAuthentication()
                .RunAsContainer(c =>
                {
                    c.WithPgAdmin();
                    c.WithPgDumpCommands();
                })
                .AddDatabase("db");

var dbsetup = builder.AddProject<Projects.WaitForSandbox_DbSetup>("dbsetup")
                     .WithReference(db).WaitFor(db);

var backend = builder.AddProject<Projects.WaitForSandbox_ApiService>("api")
                     .WithExternalHttpEndpoints()
                     .WithHttpHealthCheck("/health")
                     .WithReference(db).WaitFor(db)
                     .WaitForCompletion(dbsetup)
                     .WithReplicas(2);

builder.AddProject<Projects.WaitFor_Frontend>("frontend")
       .WithReference(backend).WaitFor(backend);

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();

public static class PgDumpExtensions
{
    public static IResourceBuilder<PostgresServerResource> WithPgDumpCommands(this IResourceBuilder<PostgresServerResource> builder)
    {
        builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((e, ct) =>
        {
            var executor = e.Services.GetRequiredService<ContainerCommandExecutor>();

            var databases = e.Model.Resources.OfType<IResourceWithParent<PostgresServerResource>>().Where(r => r.Parent == builder.Resource).ToList();
            foreach (var db in databases)
            {
                var dbBuilder = builder.ApplicationBuilder.CreateResourceBuilder(db);
                dbBuilder.WithCommand(
                    "pg-dump", // Just testing a command for now.
                    "Backup",
                    async (context) =>
                    {
                        await executor.ExecuteAsync(db.Parent, $"touch", ["/foo"], context.CancellationToken);
                        return new ExecuteCommandResult { Success = true };
                    });
            }

            return Task.CompletedTask;
        });

        return builder;
    }
}
