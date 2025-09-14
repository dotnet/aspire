using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add your distributed resources or projects here. Example:
// builder.AddProject<SomeService>("someservice");

builder.Build().Run();
