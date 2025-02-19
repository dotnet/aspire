using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Storage;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator()
.ConfigureInfrastructure((infrastructure) =>
    {
        var storageAccount = infrastructure.GetProvisionableResources().OfType<StorageAccount>().FirstOrDefault(r => r.BicepIdentifier == "storage")
            ?? throw new InvalidOperationException($"Could not find configured storage account with name 'storage'");

        // Storage Account Contributor and Storage Blob Data Owner roles are required by the Azure Functions host
        var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
        var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
        infrastructure.Add(storageAccount.CreateRoleAssignment(StorageBuiltInRole.StorageAccountContributor, principalTypeParameter, principalIdParameter));
        infrastructure.Add(storageAccount.CreateRoleAssignment(StorageBuiltInRole.StorageBlobDataOwner, principalTypeParameter, principalIdParameter));
    });
var queue = storage.AddQueues("queue");
var blob = storage.AddBlobs("blob");
var eventHub = builder.AddAzureEventHubs("eventhubs")
    .RunAsEmulator()
    .AddHub("myhub");
#if !SKIP_UNSTABLE_EMULATORS
var serviceBus = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator();
serviceBus.AddServiceBusQueue("myqueue");
var cosmosDb = builder.AddAzureCosmosDB("cosmosdb")
    .RunAsEmulator();
var database = cosmosDb.AddCosmosDatabase("mydatabase");
database.AddContainer("mycontainer", "/id");

#endif

var funcApp = builder.AddAzureFunctionsProject<Projects.AzureFunctionsEndToEnd_Functions>("funcapp")
    .WithExternalHttpEndpoints()
    .WithReference(eventHub).WaitFor(eventHub)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
#endif
    .WithReference(blob)
    .WithReference(queue)
    .WithExternalHttpEndpoints()
    .WithHostStorage(storage)
    .PublishAsAzureContainerAppWithKind((infrastructure, app) =>
    {
        app.Kind = "functionapp";
    });

builder.AddProject<Projects.AzureFunctionsEndToEnd_ApiService>("apiservice")
    .WithReference(eventHub).WaitFor(eventHub)
#if !SKIP_UNSTABLE_EMULATORS
    .WithReference(serviceBus).WaitFor(serviceBus)
    .WithReference(cosmosDb).WaitFor(cosmosDb)
#endif
    .WithReference(queue)
    .WithReference(blob)
    .WithReference(funcApp)
    .WithExternalHttpEndpoints();

builder.Build().Run();
