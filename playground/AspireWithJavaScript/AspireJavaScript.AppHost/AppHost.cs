var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

var weatherApi = builder.AddProject<Projects.AspireJavaScript_MinimalApi>("weatherapi")
    .WithExternalHttpEndpoints();

var reactvite = builder.AddViteApp("reactvite", "../AspireJavaScript.Vite")
    .WithNpm(install: true)
    .WithReference(weatherApi)
    .WithEnvironment("BROWSER", "none")
    .WithExternalHttpEndpoints();

weatherApi.PublishWithContainerFiles(reactvite, "./wwwroot");

builder.Build().Run();
