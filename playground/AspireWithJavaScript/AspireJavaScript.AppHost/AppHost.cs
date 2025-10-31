var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject<Projects.AspireJavaScript_MinimalApi>("weatherapi")
    .WithExternalHttpEndpoints();

builder.AddJavaScriptApp("angular", "../AspireJavaScript.Angular", runScriptName: "start")
    .WithReference(weatherApi)
    .WaitFor(weatherApi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.AddJavaScriptApp("react", "../AspireJavaScript.React", runScriptName: "start")
    .WithReference(weatherApi)
    .WaitFor(weatherApi)
    .WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.AddJavaScriptApp("vue", "../AspireJavaScript.Vue")
    .WithRunScript("start")
    .WithNpm(installCommand: "ci") // Use 'npm ci' for clean install, requires lock file
    .WithReference(weatherApi)
    .WaitFor(weatherApi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

var reactvite = builder.AddViteApp("reactvite", "../AspireJavaScript.Vite")
    .WithReference(weatherApi)
    .WithEnvironment("BROWSER", "none")
    .WithExternalHttpEndpoints();

builder.AddNodeApp("node", "../AspireJavaScript.NodeApp", "app.js")
    .WithRunScript("dev") // Use 'npm run dev' for development
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

weatherApi.PublishWithContainerFiles(reactvite, "./wwwroot");

builder.Build().Run();
