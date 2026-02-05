#:sdk Aspire.AppHost.Sdk@13.1.0
#:package Aspire.Hosting.Python@13.1.0
#:package Aspire.Hosting.Azure.AIFoundry@!!REPLACE_WITH_LATEST_VERSION!!

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
