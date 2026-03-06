var builder = DistributedApplication.CreateBuilder(args);

// The backend service exposes multiple health checks through a single /health endpoint
// WithHttpHealthCheck() automatically detects and parses the Aspire JSON format
builder.AddProject<Projects.HealthCheckExample_Backend>("backend")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
