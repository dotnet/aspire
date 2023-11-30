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

builder.AddProject<Projects.OrleansServer>("silo")
       .AddResource(orleans);

builder.AddProject<Projects.FrontEnd>("frontend")
       .AddResource(orleans);

using var app = builder.Build();

await app.RunAsync();

