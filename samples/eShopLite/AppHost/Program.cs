var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var catalogDb = builder.AddPostgresContainer("postgres").AddDatabase("catalogdb");
var ratingsDb = builder.AddAzureCosmosDB("cosmosdb").AddDatabase("ratingsdb");

var basketCache = builder.AddRedisContainer("basketcache");

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                     .WithReference(catalogDb)
                     .WithReplicas(2);

var ratingsService = builder.AddProject<Projects.RatingsService>("ratingsservice")
    .WithReference(ratingsDb);

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
       .WithReference(catalogDb);

builder.Build().Run();
