var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.BuildFailure_ApiService>("apiservice");

builder.Build().Run();
