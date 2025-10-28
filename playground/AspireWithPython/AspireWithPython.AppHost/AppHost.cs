#pragma warning disable ASPIREHOSTINGPYTHON001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env");

var pythonScript = builder.AddPythonScript("instrumented-python-app", "../InstrumentedPythonProject", "app.py")
       .WithUvEnvironment()
       .WithHttpEndpoint(env: "PORT")
       .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
{
    pythonScript.WithEnvironment("DEBUG", "True");
}

var backend = builder.AddUvicornApp("app", "../app", "app:app")
    .WithUvEnvironment()
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.AddViteApp("frontend", "../frontend")
    .WithNpmPackageManager()
    .WithReference(backend)
    .WaitFor(backend);

builder.Build().Run();
