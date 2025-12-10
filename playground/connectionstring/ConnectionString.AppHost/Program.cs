var builder = DistributedApplication.CreateBuilder(args);

// Add Azure Event Hubs (using connection string)
// var eventHubs = builder.AddAzureEventHubs("eh");

// var eventHub = eventHubs.AddHub("hub1");
// var consumerGroup = eventHub.AddConsumerGroup("group1");

// Add Azure Service Bus (using connection string)
// var serviceBus = builder.AddAzureServiceBus("servicebus");

// var queue1 = serviceBus.AddServiceBusQueue("queue1");
// var topic1 = serviceBus.AddServiceBusTopic("topic1");
// var subscription1 = topic1.AddServiceBusSubscription("subscription1");

// Add Azure Cosmos DB
var cosmos = builder.AddAzureCosmosDB("cosmos")
    //.RunAsEmulator(emulator => emulator.WithPartitionCount(1))
    ;
var cosmosdatabase = cosmos.AddCosmosDatabase("cosmosdb");
cosmosdatabase.AddContainer("container1", "/partitionKey");

// Add Azure Storage
// var storage = builder.AddAzureStorage("storage");

// var blobs = storage.AddBlobs("blobs");
// var blobContainer = storage.AddBlobContainer("mycontainer");
// var queues = storage.AddQueues("queues");
// var queue = storage.AddQueue("myqueue");
// var tables = storage.AddTables("tables");

// Add Azure SQL
// var sqlServer = builder.AddAzureSqlServer("sqlserver")
//     .RunAsContainer();

// var sqlDb = sqlServer.AddDatabase("sqldb");

// Add Azure Managed Redis
// var redis = builder.AddAzureManagedRedis("redis");
//     // .RunAsContainer();

// Add Azure AI Search
// var search = builder.AddAzureSearch("search");

// Add Azure SignalR
// var signalR = builder.AddAzureSignalR("signalr");

// Add Azure Kusto
// var kusto = builder.AddAzureKustoCluster("kusto")
//     // .RunAsEmulator()
//     .AddReadWriteDatabase("testdb");

// Add Azure Key Vault
// var keyVault = builder.AddAzureKeyVault("keyvault");

builder.AddNodeApp("typescript", "../ConnectionString.TypeScript", "dist/index.js")
    // .WithReference(eventHubs).WithReference(eventHub).WithReference(consumerGroup).WaitFor(eventHubs)
    // .WithReference(serviceBus).WithReference(queue1).WithReference(topic1).WithReference(subscription1).WaitFor(serviceBus)
    // .WithReference(blobs).WithReference(blobContainer).WithReference(queues).WithReference(queue).WithReference(tables).WaitFor(storage)
     .WithReference(cosmos).WithReference(cosmosdatabase).WaitFor(cosmos)
    // .WithReference(sqlServer).WithReference(sqlDb).WaitFor(sqlDb)
    // .WithReference(redis).WaitFor(redis)
    // .WithReference(search).WaitFor(search)
    // .WithReference(signalR).WaitFor(signalR)
    // .WithReference(kusto).WaitFor(kusto)
    // .WithReference(keyVault).WaitFor(keyVault)
    .WithHttpEndpoint(env: "PORT");

builder.Build().Run();
