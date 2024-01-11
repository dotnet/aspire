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

using var app = builder.Build();

await app.RunAsync();

