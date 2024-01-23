using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage");

if (builder.Environment.IsDevelopment())
{
    storage.UseEmulator();
}
else
{
    builder.AddAzureProvisioning();
}

var clusteringTable = storage.AddTables("clustering");
var grainStorage = storage.AddBlobs("grainstate");

var orleans = builder.AddOrleans("my-app")
                     .WithClustering(clusteringTable)
                     .WithGrainStorage("Default", grainStorage);

// For local development, instead of using the emulator,
// one can use the in memory provider from Orleans:
//
//var orleans = builder.AddOrleans("my-app")
//                     .WithLocalhostClustering()
//                     .WithInMemoryGrainStorage("Default");

builder.AddProject<Projects.OrleansServer>("silo")
       .WithReference(orleans);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to WorkloadAttributes.cs
// for the path to the dashboard binary (defaults to a relative path to the artifacts
// directory).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard)
    .WithEnvironment("DOTNET_DASHBOARD_GRPC_ENDPOINT_URL", "http://localhost:5555")
    .ExcludeFromManifest();

using var app = builder.Build();

await app.RunAsync();

