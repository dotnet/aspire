// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var openai = builder.AddConnectionString("openai");

builder.AddProject<Projects.OpenAIEndToEnd_WebStory>("webstory")
       .WithReference(openai);

builder.Build().Run();
