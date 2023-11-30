using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<AzureTableStorageResource> clusteringTable;
IResourceBuilder<AzureBlobStorageResource> grainStorage;

var storage = builder.AddAzureStorage("storage");

if (builder.Environment.IsDevelopment())
{
    storage.UseEmulator();
}
else
{
    builder.AddAzureProvisioning();
}

clusteringTable = storage.AddTables("clustering");
grainStorage = storage.AddBlobs("grainstate");

var orleans = builder.AddOrleans("my-app")
                     .WithClustering(clusteringTable)
                     .WithGrainStorage("Default", grainStorage);

builder.AddProject<Projects.OrleansServer>("silo")
       .WithOrleansServer(orleans);

builder.AddProject<Projects.FrontEnd>("frontend")
       .WithOrleansClient(orleans);

using var app = builder.Build();

await app.RunAsync();

