var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var cosmosdb = builder.AddAzureCosmosDB("cosmosdb");
var catalogdb = cosmosdb.AddDatabase("catalogdb");
var ratingsdb = cosmosdb.AddDatabase("ratingsdb");

var basketCache = builder.AddRedisContainer("basketCache");

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
    .WithReference(catalogdb);

var ratingsService = builder.AddProject<Projects.RatingsService>("ratingsservice")
    .WithReference(ratingsdb);

var messaging = builder.AddRabbitMQContainer("messaging");

var basketService = builder.AddProject<Projects.BasketService>("basketservice")
                    .WithReference(basketCache)
                    .WithReference(messaging);

builder.AddProject<Projects.MyFrontend>("frontend")
       .WithReference(basketService)
       .WithReference(catalogService.GetEndpoint("http"))
       .WithReference(ratingsService.GetEndpoint("http"));

builder.AddProject<Projects.OrderProcessor>("orderprocessor")
       .WithReference(messaging)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basketService)
       .WithReference(catalogService)
       .WithReference(ratingsService);

builder.AddProject<Projects.CatalogDb>("catalogdbapp")
       .WithReference(catalogdb);

builder.Build().Run();
