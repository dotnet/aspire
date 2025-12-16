// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage-account");

var account = builder.AddAzureCognitiveServicesAccount("cogsvc-account")
    ;

var deployment = account.AddDeployment("my-gpt-5", "OpenAI", "gpt-5");
var project = account.AddProject("cogsvc-project");

var app = builder.AddUvicornApp("app", "./app", "main:app")
    .WithUv()
    .WithHttpEndpoint()
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(project)
    .WithReference(deployment)
    .WaitFor(deployment)
    .WithDeploymentImageTag((ctx) =>
    {
        return "latest";
    })
    .PublishAsHostedAgent(project, (opts) =>
    {
        opts.Description = "Foundry Agent Basic Example";
        opts.Metadata["managed-by"] = "aspire-foundry";
        opts.Definition.Cpu = "2";
        opts.Definition.Memory = "8GiB";
    });

// var frontend = builder.AddViteApp("frontend", "./frontend")
//     .WithReference(app)
//     .WaitFor(app);

// app.PublishWithContainerFiles(frontend, "./static");

builder.Build().Run();
