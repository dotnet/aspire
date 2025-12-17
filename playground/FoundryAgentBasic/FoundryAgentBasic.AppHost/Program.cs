using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var suffix = builder.Configuration.GetSection("App").GetValue("suffix", "basic");
var registry = builder.AddAzureContainerRegistry($"myregistry{suffix}");
var account = builder.AddAzureCognitiveServicesAccount($"cogsvc-account-{suffix}");

var deployment = account.AddDeployment("my-gpt-5", "OpenAI", "gpt-4.1-mini", "2025-04-14");
var project = account.AddProject($"proj-{suffix}");
var app = builder.AddPythonApp($"app-{suffix}", "../app", "main.py")
    .WithUv()
    .WithHttpEndpoint(port: 9999, name: "api")
    .WithExternalHttpEndpoints()
    .WithReference(project)
    .WithReference(deployment)
    .WaitFor(deployment)
    .PublishAsHostedAgent(project, (opts) =>
    {
        opts.Description = "Foundry Agent Basic Example";
    });

builder.Build().Run();
