using Aspire.Hosting.Azure.Kusto;

var builder = DistributedApplication.CreateBuilder(args);

var kusto = builder.AddAzureKustoCluster("kusto")
    .RunAsEmulator();
var db = kusto.AddDatabase("testdb");

builder.AddProject<Projects.AzureKusto_Worker>("worker")
    .WithReference(db)
    .WaitFor(db);

var app = builder.Build();
app.Run();
