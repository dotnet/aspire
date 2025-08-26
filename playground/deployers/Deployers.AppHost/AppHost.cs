#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIRECOSMOSDB001

var builder = DistributedApplication.CreateBuilder(args);

var aca = builder.AddAzureContainerAppEnvironment("aca-env");
var aas = builder.AddAzureAppServiceEnvironment("aas-env");

var storage = builder.AddAzureStorage("storage");

var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("foobarbaz");
var myBlobContainer = storage.AddBlobContainer("myblobcontainer");

var eventHub = builder.AddAzureEventHubs("eventhubs")
    .RunAsEmulator()
    .AddHub("myhub");
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();
serviceBus.AddServiceBusQueue("myqueue");
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsPreviewEmulator();
var database = cosmosDb.AddCosmosDatabase("mydatabase");
database.AddContainer("mycontainer", "/id");

builder.AddRedis("cache")
    .WithComputeEnvironment(aca);

builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("functions-api-service")
    .WithExternalHttpEndpoints()
    .WithComputeEnvironment(aas)
    .WithExternalHttpEndpoints()
    .WithReference(eventHub).WaitFor(eventHub)
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
    .WithReference(queue)
    .WithReference(blob);

builder.AddProject<Projects.Deployers_ApiService>("api-service")
    .WithExternalHttpEndpoints()
    .WithComputeEnvironment(aas);

builder.AddDockerfile("python-app", "../Deployers.Dockerfile")
    .WithHttpEndpoint(targetPort: 80)
    .WithExternalHttpEndpoints()
    .WithComputeEnvironment(aca);

builder.AddAzureFunctionsProject<Projects.AzureFunctionsEndToEnd_Functions>("func-app")
    .WithExternalHttpEndpoints()
    .WithComputeEnvironment(aca)
    .WithReference(eventHub).WaitFor(eventHub)
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
    .WithReference(myBlobContainer).WaitFor(myBlobContainer)
    .WithReference(blob)
    .WithReference(queue);

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
