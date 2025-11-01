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
