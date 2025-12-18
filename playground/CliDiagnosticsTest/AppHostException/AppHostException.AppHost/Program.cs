// This AppHost throws an exception during startup to test error handling

var builder = DistributedApplication.CreateBuilder(args);

// Simulate an exception during resource configuration
throw new InvalidOperationException("Simulated AppHost exception during resource configuration. This tests the CLI's error handling and diagnostics bundle creation.");

builder.Build().Run();
