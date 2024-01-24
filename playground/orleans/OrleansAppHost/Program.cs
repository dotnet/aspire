using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage");

if (!args.Contains("--publisher")) // AZD UP passes in --publisher manifest
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
//                     .WithDevelopmentClustering()
//                     .WithMemoryGrainStorage("Default");

builder.AddProject<Projects.OrleansServer>("silo")
       .WithReference(orleans);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard)
    .WithEnvironment("DOTNET_RESOURCE_SERVICE_ENDPOINT_URL", "http://localhost:5555")
    .ExcludeFromManifest();

using var app = builder.Build();

await app.RunAsync();

