#:sdk Aspire.AppHost.Sdk@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Azure@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Azure.AIFoundry@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Python@!!REPLACE_WITH_LATEST_VERSION!!
#:package Microsoft.Extensions.Configuration@10.0.2

var builder = DistributedApplication.CreateBuilder(args);

var name = builder.Configuration.GetSection("App").GetValue<string>("Name") ?? "my-app";
var project = builder.AddAzureAIFoundryProject($"{name}-proj");

builder.AddPythonApp(name, "../agent", "main.py")
    .WithUv()
    .WithHttpEndpoint(port: 9999, name: "api", env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(project)
    .WithReference(project.AddModelDeployment($"gpt41mini", AIFoundryModel.OpenAI.Gpt41Mini))
    .PublishAsHostedAgent();

builder.Build().Run();
