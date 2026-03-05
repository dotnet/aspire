#:sdk Aspire.AppHost.Sdk@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Azure@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Foundry@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Python@!!REPLACE_WITH_LATEST_VERSION!!
#:package Microsoft.Extensions.Configuration@10.0.2

var builder = DistributedApplication.CreateBuilder(args);

var name = builder.Configuration.GetSection("App").GetValue<string>("Name") ?? "my-app";
var project = builder.AddFoundryProject("proj");
var chat = project.AddModelDeployment("chat", FoundryModel.OpenAI.Gpt41Mini);

builder.AddPythonApp(name, "../agent", "main.py")
    .WithUv()
    .WithReference(project)
    .WithReference(chat)
    .PublishAsHostedAgent();

builder.Build().Run();
