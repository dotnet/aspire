var builder = DistributedApplication.CreateBuilder(args);

// This intentionally references a project that doesn't exist to cause a build failure
// Uncomment the line below to test build failures:
// var apiService = builder.AddProject<Projects.NonExistent>("apiservice");

// For now, just create a minimal app that runs
var redis = builder.AddRedis("cache");

builder.Build().Run();
