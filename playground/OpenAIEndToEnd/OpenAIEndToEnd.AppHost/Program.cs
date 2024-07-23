// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var deploymentAndModelName = "gpt-4o";
var openai = builder.AddAzureOpenAI("openai").AddDeployment(
    // the default SKU capacity of 1,000 TPM is not high enough for the OpenAIEndToEnd_WebStory project
    // see https://github.com/dotnet/aspire/issues/4970
    new(deploymentAndModelName, deploymentAndModelName, "2024-05-13", skuCapacity: 8)
    );

builder.AddProject<Projects.OpenAIEndToEnd_WebStory>("webstory")
       .WithExternalHttpEndpoints()
       .WithReference(openai)
       .WithEnvironment("OpenAI__DeploymentName", deploymentAndModelName);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
