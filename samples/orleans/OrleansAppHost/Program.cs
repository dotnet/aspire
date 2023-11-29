var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var storage = builder.AddAzureStorage("storage");
var clusteringTable = storage.AddTables("clustering");
var grainStorage = storage.AddBlobs("grainstate");

var orleans = builder.AddOrleans("my-app")
                     .WithClustering(clusteringTable)
                     .WithGrainStorage("Default", grainStorage);

builder.AddProject<Projects.OrleansServer>("silo")
       .WithOrleansServer(orleans);

builder.AddProject<Projects.FrontEnd>("frontend")
       .WithOrleansClient(orleans);

using var app = builder.Build();

await app.RunAsync();

