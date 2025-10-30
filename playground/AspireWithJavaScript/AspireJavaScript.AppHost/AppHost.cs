var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject<Projects.AspireJavaScript_MinimalApi>("weatherapi")
    .WithExternalHttpEndpoints();

builder.AddJavaScriptApp("angular", "../AspireJavaScript.Angular")
    .WithReference(weatherApi)
    .WaitFor(weatherApi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.AddJavaScriptApp("react", "../AspireJavaScript.React")
    .WithReference(weatherApi)
    .WaitFor(weatherApi)
    .WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.AddJavaScriptApp("vue", "../AspireJavaScript.Vue")
    .WithNpm(installArgs: ["ci"]) // Use 'npm ci' for clean install
    .WithReference(weatherApi)
    .WaitFor(weatherApi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

var reactvite = builder.AddViteApp("reactvite", "../AspireJavaScript.Vite")
    .WithReference(weatherApi)
    .WithEnvironment("BROWSER", "none")
    .WithExternalHttpEndpoints();

weatherApi.PublishWithContainerFiles(reactvite, "./wwwroot");

builder.Build().Run();
