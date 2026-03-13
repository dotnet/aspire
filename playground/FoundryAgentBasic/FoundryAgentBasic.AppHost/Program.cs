using Aspire.Hosting.Foundry;

var builder = DistributedApplication.CreateBuilder(args);

var project = builder.AddFoundryProject("proj");
project.AddModelDeployment("chat", FoundryModel.OpenAI.Gpt41Mini);

builder.Build().Run();
