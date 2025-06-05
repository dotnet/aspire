var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();
app.MapDefaultEndpoints();

app.MapGet("/", () => Environment.GetEnvironmentVariable("ECHO_TEXT"));

app.Run();
