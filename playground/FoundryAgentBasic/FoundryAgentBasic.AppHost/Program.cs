using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
var name = builder.Configuration.GetSection("App").GetValue<string>("Name") ?? "my-app";

var project = builder.AddAzureAIFoundryProject("proj");
var chat = project.AddModelDeployment("chat", AIFoundryModel.OpenAI.Gpt41Mini);

builder.AddPythonApp(name, "../app", "main.py")
    .WithHttpEndpoint(port: 9999, name: "api", env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(project)
    .WithReference(chat)
    .PublishAsHostedAgent();

builder.Build().Run();
