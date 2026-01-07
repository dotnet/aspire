using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var foundry = builder.AddAzureAIFoundry("my-app-foundry");
var project = foundry.AddProject("my-app-project");
var app = builder.AddPythonApp("my-app", "../app", "main.py")
    .WithUv()
    .WithHttpEndpoint(port: 9999, name: "api")
    .WithExternalHttpEndpoints()
    .WithReference(project)
    .WithReference(foundry.AddDeployment("my-gpt-5", AIFoundryModel.OpenAI.Gpt5))
    .PublishAsHostedAgent((opts) =>
    {
        opts.Description = "Foundry Agent Basic Example";
    });

builder.Build().Run();
