var builder = DistributedApplication.CreateBuilder(args);

var token = builder.AddParameter("milvusauth", true);

var milvusdb = builder.AddMilvus("milvus", token)
    .WithDataVolume("milvus-data")
    .WithAttu();

builder.AddProject<Projects.MilvusPlayground_ApiService>("apiservice")
    .WithReference(milvusdb);

builder.Build().Run();
