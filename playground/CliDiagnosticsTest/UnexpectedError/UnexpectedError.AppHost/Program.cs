// This AppHost is intentionally malformed to cause unexpected CLI errors

var builder = DistributedApplication.CreateBuilder(args);

// Create a resource that will cause issues during CLI processing
// This simulates unexpected errors in the CLI infrastructure itself
builder.AddResource(null!); // Null reference to trigger unexpected error

builder.Build().Run();
