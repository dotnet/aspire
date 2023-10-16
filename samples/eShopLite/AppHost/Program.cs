var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var grafana = builder.AddContainer("grafana", "grafana/grafana")
                     .WithServiceBinding(containerPort: 3000, name: "grafana-http", scheme: "http");

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
       .WithReference(catalog.GetEndpoint("http"))
       .WithEnvironment("GRAFANA_URL", () => grafana.GetEndpoint("grafana-http").UriStringOrPlaceholder);

builder.AddProject<Projects.OrderProcessor>("orderprocessor")
       .WithReference(serviceBus, optional: true)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basket)
       .WithReference(catalog);

builder.AddContainer("prometheus", "prom/prometheus")
       .WithVolumeMount("../prometheus", "/etc/prometheus")
       .WithServiceBinding(9090);

builder.Build().Run();
