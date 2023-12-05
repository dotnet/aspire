var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgresContainer("postgres").AddDatabase("catalogdb");

var ratingsdb1 = builder.AddMySqlServer("dbserver1").AddDatabase("ratings1");
var ratingsdb2 = builder.AddMySqlContainer("dbserver2").AddDatabase("ratings2");

var basketCache = builder.AddRedisContainer("basketcache");
builder.AddRedis("abstractbasketcache"); // TODO: Tidy up, just testing.

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                     .WithReference(catalogDb)
                     .WithReplicas(2)
                     .WithReference(ratingsdb1)
                     .WithReference(ratingsdb2);

var messaging = builder.AddRabbitMQContainer("messaging");

var basketService = builder.AddProject("basketservice", @"..\BasketService\BasketService.csproj")
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
       .WithReference(catalogDb);

builder.Build().Run();
