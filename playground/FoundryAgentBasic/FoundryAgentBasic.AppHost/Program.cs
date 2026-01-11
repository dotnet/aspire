using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);
var name = builder.Configuration.GetSection("App").GetValue<string>("Name") ?? "my-app";

var project = builder.AddFoundryProject($"{name}-proj");

builder.AddPythonApp(name, "../app", "main.py")
    .WithHttpEndpoint(port: 9999, name: "api", env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(project)
    .WithReference(project.AddModelDeployment($"{name}-gpt41mini", AIFoundryModel.OpenAI.Gpt41Mini))
    .PublishAsHostedAgent();

builder.Build().Run();
