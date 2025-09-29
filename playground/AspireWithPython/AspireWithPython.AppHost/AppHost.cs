using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIREHOSTINGPYTHON001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var pythonapp = builder.AddPythonApp("instrumented-python-app", "../InstrumentedPythonProject", "app.py")
       .WithHttpEndpoint(env: "PORT")
       .WithExternalHttpEndpoints();
#pragma warning restore ASPIREHOSTINGPYTHON001

if (builder.ExecutionContext.IsRunMode && builder.Environment.IsDevelopment())
{
    pythonapp.WithEnvironment("DEBUG", "True");
}

builder.Build().Run();
