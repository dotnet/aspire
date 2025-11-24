// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using DotnetTool.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDotnetTool("ef", "dotnet-ef", "dotnet-ef");

// Multiple versions
builder.AddDotnetTool("dump1", "dotnet-dump", "dotnet-dump")
    .WithArgs("--version")
    .WithPackageVersion("9.0.652701");
builder.AddDotnetTool("dump2", "dotnet-dump", "dotnet-dump")
    .WithPackageVersion("9.0.621003")
    .WithArgs("--version");

// Concurrency
for (int i = 0; i < 5; i++)
{
    builder.AddDotnetTool($"trace-{i}", "dotnet-trace", "dotnet-trace")
        .WithArgs("--version");
}

foreach(var resource in builder.Resources.OfType<DotnetToolResource>())
{
    builder.CreateResourceBuilder(resource)
        .WithCommand("reset", "Reset", ctx =>
        {
            try
            {
                var path = Path.GetDirectoryName(resource.Command);
                Directory.Delete(path!, true);
                return Task.FromResult(CommandResults.Success());
            }
            catch (Exception ex)
            {
                return Task.FromResult(CommandResults.Failure(ex));
            }
        }, new CommandOptions
        {
            IconName = "ArrowReset"
        });
}

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
