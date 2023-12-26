var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgres("postgres").AddDatabase("catalogdb");

var basketCache = builder.AddRedis("basketcache");

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                     .WithReference(catalogDb)
                     .WithReplicas(2);

var messaging = builder.AddRabbitMQ("messaging");

var basketService = builder.AddProject("basketservice", @"..\BasketService\BasketService.csproj")
                    .WithReference(basketCache)
                    .WithReference(messaging);

var endpoint = catalogService.GetEndpoint("http");

builder.AddProject<Projects.MyFrontend>("frontend")
       .WithReference(basketService)
       .WithReference(endpoint)
       .WithEnvironment("ENV_DB", $"{catalogDb}")
       .WithEnvironment("ENV_URI", $"{endpoint}")
       .WithEnvironment("ENV_DBCONN", $"{catalogDb.Resource}");

builder.AddProject<Projects.OrderProcessor>("orderprocessor")
       .WithReference(messaging)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basketService)
       .WithReference(catalogService);

builder.AddProject<Projects.CatalogDb>("catalogdbapp")
       .WithReference(catalogDb);

builder.Build().Run();
