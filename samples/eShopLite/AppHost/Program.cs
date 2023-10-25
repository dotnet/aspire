var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var catalogdb = builder.AddAzureCosmosDB("cosmosdb").AddDatabase("catalogdb");

var basketCache = builder.AddRedisContainer("basketCache");

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
    .WithAzureCosmosDB(catalogdb);

var ordersQueue = builder.AddAzureServiceBus("messaging", queueNames: ["orders"]);

var basketService = builder.AddProject<Projects.BasketService>("basketservice")
                    .WithReference(basketCache)
                    .WithReference(ordersQueue, optional: true);

builder.AddProject<Projects.MyFrontend>("frontend")
       .WithReference(basketService)
       .WithReference(catalogService.GetEndpoint("http"));

builder.AddProject<Projects.OrderProcessor>("orderprocessor")
       .WithReference(ordersQueue, optional: true)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basketService)
       .WithReference(catalogService);

builder.AddProject<Projects.CatalogDb>("catalogdbapp")
       .WithReference(catalogdb)
       .WithEnvironment("Aspire.Microsoft.EntityFrameworkCore.Cosmos:DatabaseName", catalogdb.Resource.Name);

builder.Build().Run();
