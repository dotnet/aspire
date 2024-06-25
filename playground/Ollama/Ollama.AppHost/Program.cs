var builder = DistributedApplication.CreateBuilder(args);

var ollama = builder.ExecutionContext.IsRunMode
    ? builder.AddOllama("ollama")
        .AddModel("llama2:latest")
        .AddModel("phi3:latest")
        .WithDataVolume("ollama")
        .WithOpenWebUI()
    : builder.AddConnectionString("ollama");

builder.AddProject<Projects.Ollama_ApiService>("apiservice")
    .WithReference(ollama);

builder.Build().Run();
