
var builder = DistributedApplication.CreateBuilder(args);
var suffix = "5kaobtwfx0k";
var registry = builder.AddAzureContainerRegistry($"myregistry-{suffix}").RunAsExisting($"myregistry-{suffix}", null).PublishAsExisting($"myregistry-{suffix}", null);
var account = builder.AddAzureCognitiveServicesAccount($"cogsvc-account-{suffix}").RunAsExisting($"cogsvc-account-{suffix}", null).PublishAsExisting($"cogsvc-account-{suffix}", null);

var deployment = account.AddDeployment("my-gpt-5", "OpenAI", "gpt-4.1-mini", "2025-04-14").RunAsExisting("my-gpt-5", null).PublishAsExisting("my-gpt-5", null);
var project = account.AddProject($"proj-{suffix}").RunAsExisting($"proj-{suffix}", null).PublishAsExisting($"proj-{suffix}", null);
var app = builder.AddPythonApp($"app-{suffix}", "../app", "main.py")
    .WithUv()
    .WithHttpEndpoint(port: 9999, name: "api")
    .WithExternalHttpEndpoints()
    .WithReference(project)
    .WithReference(deployment)
    .WaitFor(deployment)
    .WithDeploymentImageTag((ctx) =>
    {
        return "latest";
    })
    //.PublishAsDockerFile()
    .PublishAsHostedAgent(project, (opts) =>
    {
        opts.Description = "Foundry Agent Basic Example";
        opts.Metadata["managed-by"] = "aspire-foundry";
        opts.Definition.Cpu = "2";
        opts.Definition.Memory = "8GiB";
    });
/*
var frontend = builder.AddViteApp("frontend", "./frontend")
    .WithReference(app)
    .WaitFor(app);

app.PublishWithContainerFiles(frontend, "./static");
*/
builder.Build().Run();
