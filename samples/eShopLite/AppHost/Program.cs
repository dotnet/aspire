var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var catalogdb = builder.AddPostgresContainer("postgres").AddDatabase("catalog");

var redis = builder.AddRedisContainer("basketCache");

var catalog = builder.AddProject<Projects.CatalogService>("catalogservice")
                     .WithReference(catalogdb)
                     .WithReplicas(2);

var serviceBus = builder.AddAzureServiceBus("messaging", queueNames: ["orders"]);

var basket = builder.AddProject<Projects.BasketService>("basketservice")
                    .WithReference(redis)
                    .WithReference(serviceBus, optional: true);

builder.AddProject<Projects.MyFrontend>("myfrontend")
       .WithReference(basket)
       .WithReference(catalog.GetEndpoint("http"));

builder.AddProject<Projects.OrderProcessor>("orderprocessor")
       .WithReference(serviceBus, optional: true)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basket)
       .WithReference(catalog);

builder.Build().Run();
