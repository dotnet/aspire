// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

#pragma warning disable ASPIRE0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var openai = builder.AddAzureOpenAI("openai", (_, _, _, deployments) => {
    var deployment = deployments.Single();
    deployment.AddOutput("modelName", x => x.Name);
}).AddDeployment(new("gpt-35-turbo", "gpt-35-turbo", "0613"));
#pragma warning restore ASPIRE0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.AddProject<Projects.OpenAIEndToEnd_WebStory>("webstory")
       .WithReference(openai)
       .WithEnvironment("OpenAI__DeploymentName", openai.GetOutput("modelName"));

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
