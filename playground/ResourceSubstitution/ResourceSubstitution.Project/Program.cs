using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => $"""
    👋🌍
    🏷️ Host: {Environment.MachineName}
    💻 OS: { RuntimeInformation.OSDescription }
    🪪 PID: {Environment.ProcessId}
    """);
app.MapGet("/health", () => "💓 Healthy");

app.Run();
