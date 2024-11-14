// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.CognitiveServices;

var builder = DistributedApplication.CreateBuilder(args);

var openaiA = builder.AddAzureOpenAI("openaiA")
    .ConfigureInfrastructure(infra =>
    {
         var cognitiveAccount = infra.GetProvisionableResources().OfType<CognitiveServicesAccount>().Single();
        cognitiveAccount.Properties.DisableLocalAuth = false;
    })
    .AddDeployment(new("modelA1", "gpt-4o", "2024-05-13"))
    ;

//var openaiB = builder.AddAzureOpenAI("openaiB")
//    .ConfigureInfrastructure(infra =>
//    {
//        var cognitiveAccount = infra.GetProvisionableResources().OfType<CognitiveServicesAccount>().Single();
//        cognitiveAccount.Properties.DisableLocalAuth = false;
//    })
//    .AddDeployment(new("modelB1", "gpt-4o", "2024-05-13"))
//    .AddDeployment(new("modelB2", "gpt-4o", "2024-05-13"));

builder.AddProject<Projects.OpenAIEndToEnd_WebStory>("webstory")
       .WithExternalHttpEndpoints()
       .WithReference(openaiA)
       //.WithReference(openaiB)
       ;

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
