// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var openai = builder.AddAzureOpenAIConstruct("openai")
                    .AddDeployment(new("gpt-35-turbo", "gpt-35-turbo", "0613"));

builder.AddProject<Projects.OpenAIEndToEnd_WebStory>("webstory")
       .WithReference(openai);

builder.Build().Run();
