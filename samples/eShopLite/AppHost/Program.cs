var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var catalogdb = builder.AddAzureCosmosDB("cosmosdb").AddDatabase("catalogdb");

var basketCache = builder.AddRedisContainer("basketCache");

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
    .WithReference(catalogdb);

var messaging = builder.AddRabbitMQContainer("messaging");

var basketService = builder.AddProject<Projects.BasketService>("basketservice")
                    .WithReference(basketCache)
                    .WithReference(messaging);

builder.AddProject<Projects.MyFrontend>("frontend")
       .WithReference(basketService)
       .WithReference(catalogService.GetEndpoint("http"));

builder.AddProject<Projects.OrderProcessor>("orderprocessor")
       .WithReference(messaging)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basketService)
       .WithReference(catalogService);

builder.AddProject<Projects.CatalogDb>("catalogdbapp")
       .WithReference(catalogdb);

builder.Build().Run();
