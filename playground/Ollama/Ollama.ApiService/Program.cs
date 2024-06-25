using OllamaSharp;
using OllamaSharp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var uri = app.Configuration.GetConnectionString("ollama");
var ollama = new OllamaApiClient(uri!, "llama2");

app.MapGet("/models", async () =>
{
    var models = await ollama.ListLocalModels();
    return models;
});

app.MapGet("/joke", async () =>
{
    GenerateCompletionRequest request = new()
    {
        Model = "llama2",
        Prompt = "Tell me a dad joke",
    };

    var completion = await ollama.GetCompletion(request);
    return completion.Response;
});

app.MapDefaultEndpoints();

app.Run();
