// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var chat = builder.AddGitHubModel("chat", "openai/gpt-4o-mini");

// To set the GitHub Models API key define the value for the following parameter in User Secrets.
// Alternatively, you can set the environment variable GITHUB_TOKEN and comment the line below.
chat.WithApiKey(builder.AddParameter("github-api-key", secret: true));

builder.AddProject<Projects.GitHubModelsEndToEnd_WebStory>("webstory")
       .WithExternalHttpEndpoints()
       .WithReference(chat);

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
