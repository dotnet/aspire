using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var project = builder.AddFoundryProject("my-app-project");

builder.AddPythonApp("my-app", "../app", "main.py")
    .WithUv()
    .WithHttpEndpoint(port: 9999, name: "api")
    .WithExternalHttpEndpoints()
    .WithReference(project)
    .WithReference(project.AddModelDeployment("my-gpt-5", AIFoundryModel.OpenAI.Gpt5))
    .PublishAsHostedAgent();

builder.Build().Run();
