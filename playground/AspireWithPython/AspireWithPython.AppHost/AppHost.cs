#pragma warning disable ASPIREHOSTINGPYTHON001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env");

var pythonapp = builder.AddPythonScript("instrumented-python-app", "../InstrumentedPythonProject", "app.py")
       .WithUvEnvironment()
       .WithHttpEndpoint(env: "PORT")
       .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
{
    pythonapp.WithEnvironment("DEBUG", "True");
}

builder.Build().Run();
