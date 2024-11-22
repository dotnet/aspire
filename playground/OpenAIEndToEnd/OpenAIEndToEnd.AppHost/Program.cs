// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

// var deploymentAndModelName = "gpt-4o";
// var openai = builder.AddAzureOpenAI("openai").AddDeployment(
//     new(deploymentAndModelName, deploymentAndModelName, "2024-05-13")
//     );

var openai = builder.AddGitHubModel("openai");

builder.AddProject<Projects.OpenAIEndToEnd_WebStory>("webstory")
       .WithExternalHttpEndpoints()
       .WithReference(openai)
       .WithEnvironment("OpenAI__DeploymentName", "gpt-4o-mini");

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

public static class GitHubModelExtensions
{
    public static IResourceBuilder<GitHubModelResource> AddGitHubModel(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new GitHubModelResource(name);
        var resourceBuilder = builder.AddResource(resource);
        return resourceBuilder;
    }
}

public class GitHubModelResource(string name) : Resource(name), IResourceWithConnectionString
{
    public ReferenceExpression ConnectionStringExpression => ReferenceExpression.Create($"Endpoint=https://models.inference.ai.azure.com;Key={Environment.GetEnvironmentVariable("GITHUB_TOKEN")}");
}

