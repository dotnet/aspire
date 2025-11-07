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
    .WithHttpEndpoint(env: "PORT", port: 3000)
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile()
    .WithBrowserDebugger("msedge", "./", "http://localhost:3000", props =>
    {
        props.SourceMapPathOverrides = new Dictionary<string, string>
        {
            { "webpack://react-weather/./src/*", "${webRoot}/src/*" },
        };
    });

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
    .PublishAsDockerFile()
    .WithJavaScriptDebuggerProperties(props =>
    {
        props.Trace = true;
    });

weatherApi.PublishWithContainerFiles(reactvite, "./wwwroot");

builder.Build().Run();
