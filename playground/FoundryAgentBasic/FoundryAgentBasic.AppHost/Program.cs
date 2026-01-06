using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var foundry = builder.AddAzureAIFoundry("my-app-foundry");
var app = builder.AddPythonApp("my-app", "../app", "main.py")
    .WithUv()
    .WithHttpEndpoint(port: 9999, name: "api")
    .WithExternalHttpEndpoints()
    .WithReference(foundry) // For `AIProjectClient`
    .WithReference(foundry.AddDeployment("my-gpt-5", AIFoundryModel.OpenAI.Gpt5)) // To use with `OpenAIClient`
    .PublishAsHostedAgent((opts) =>
    {
        opts.Description = "Foundry Agent Basic Example";
    });

builder.Build().Run();
