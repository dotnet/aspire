var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Hello World!");

app.Run();
