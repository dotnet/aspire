using Docker.DotNet;
using Docker.DotNet.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Docker Client API is running. Use /containers to list containers.");

app.MapGet("/ping", () => "pong");

app.MapGet("/containers", async (HttpContext context) =>
{
    try
    {
        // Connect to Docker daemon through Unix socket
        var client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock"))
            .CreateClient();
        
        // List containers
        var containers = await client.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                All = true
            });
            
        return Results.Json(new 
        { 
            status = "success", 
            containers = containers.Select(c => new 
            {
                id = c.ID,
                names = c.Names,
                image = c.Image,
                state = c.State,
                status = c.Status
            }) 
        }, 
        new JsonSerializerOptions { WriteIndented = true });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "error", message = ex.Message, stackTrace = ex.StackTrace }, 
            statusCode: 500);
    }
});

app.Run();