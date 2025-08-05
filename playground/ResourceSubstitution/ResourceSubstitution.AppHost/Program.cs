// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var regularContainer = builder.AddContainer("regularContainer", "aspire/playground/resourcesubstitution")
    .WithHttpEndpoint(targetPort: 8080, env: "ASPNETCORE_HTTP_PORTS")
    .WithHttpHealthCheck("/health");

var regularProject = builder.AddProject<Projects.ResourceSubstitution_Project>("regularProject")
    .WithEndpoint("http", x => x.Port = null)
    .WithEndpoint("https", x => x.Port = null)
    .WithHttpHealthCheck("/health");

var regularExecutable = builder.AddExecutable("regularExecutable", "dotnet", ".")
    .WithArgs("run", "--project", new Projects.ResourceSubstitution_Project().ProjectPath)
    // launch settings keep on overriding port env vars
    .WithArgs("--launch-profile", "NoApplicationUrls")
    .WithHttpEndpoint(env: "ASPNETCORE_HTTP_PORTS")
    .WithHttpsEndpoint(env: "ASPNETCORE_HTTPS_PORTS")
    .WithEnvironment("ASPNETCORE_URLS", "")
    .WithHttpHealthCheck("/health");

var rawContainer = builder.AddResource(new RawResource("rawContainer"))
    .WithAnnotation(new ContainerImageAnnotation { Image = "aspire/playground/resourcesubstitution" })
    .WithHttpEndpoint(targetPort: 8080, env: "ASPNETCORE_HTTP_PORTS")
    .WithHttpHealthCheck("/health");

//TODO: raw exe
//TODO: raw container

// Shenanigans
// This has some wacky behaviour - both exe and contaienr get started
// But dashboard only shows output from one, and it seems non deterministic
// which one is picked
var exeWithContainerAnnotation = builder.AddExecutable("shenanigansExeWithContainerAnnotation", "dotnet", ".")
    .WithArgs("run", "--project", new Projects.ResourceSubstitution_Project().ProjectPath)
    // launch settings keep on overriding port env vars
    .WithArgs("--launch-profile", "NoApplicationUrls")
    .WithHttpEndpoint(targetPort: 8080, env: "ASPNETCORE_HTTP_PORTS")
    //.WithHttpsEndpoint(targetPort: 8443, env: "ASPNETCORE_HTTPS_PORTS")
    .WithEnvironment("ASPNETCORE_URLS", "")
    .WithHttpHealthCheck("/health")
    .WithAnnotation(new ContainerImageAnnotation { Image = "aspire/playground/resourcesubstitution" });

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

internal sealed class RawResource(string name) : Resource(name), IResourceWithEndpoints
{
}