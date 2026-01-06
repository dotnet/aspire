// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var foundry = builder.AddAzureAIFoundry("my-foundry");
var deployment = foundry.AddDeployment("my-gpt-5", AIFoundryModel.OpenAI.Gpt5);
var project = foundry.AddProject("my-foundry-proj");

var kvConn = project.AddConnection(builder.AddAzureKeyVault("foundry-kv"));
var dbConn = project.AddConnection(builder.AddAzureCosmosDB("foundry-db"));
var registryConn = project.AddConnection(builder.AddAzureContainerRegistry("foundry-registry"));
var storageConn = project.AddConnection(builder.AddAzureStorage("storage-account"));
var capHost = project.AddCapabilityHost("capability-host")
    .WithReference(kvConn)
    .WithReference(dbConn)
    .WithReference(registryConn)
    .WithReference(storageConn);

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
