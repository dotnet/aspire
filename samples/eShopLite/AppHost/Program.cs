var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var catalogDb = builder.AddPostgresContainer("postgres").AddDatabase("catalogdb");

var basketCache = builder.AddRedisContainer("basketcache");

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                     .WithReference(catalogDb)
                     .WithReplicas(2);

var ordersQueue = builder.AddRabbitMQContainer("messaging");

var basketService = builder.AddProject<Projects.BasketService>("basketservice")
                    .WithReference(basketCache)
                    .WithReference(ordersQueue);

builder.AddProject<Projects.MyFrontend>("frontend")
       .WithReference(basketService)
       .WithReference(catalogService.GetEndpoint("http"));

builder.AddProject<Projects.OrderProcessor>("orderprocessor")
       .WithReference(ordersQueue)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basketService)
       .WithReference(catalogService);

builder.AddProject<Projects.CatalogDb>("catalogdbapp")
       .WithReference(catalogDb);

builder.Build().Run();
