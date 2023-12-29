var builder = DistributedApplication.CreateBuilder(args);

var prometheusPort = 9090;
builder.AddPrometheusContainer("prometheus-container", "../prometheus", "prom-data", prometheusPort);

var grafanaPort = 3000;
builder.AddGrafanaContainer("grafana-container", "../grafana/config", "../grafana/dashboards", "grafana-data", grafanaPort);

var catalogDb = builder.AddPostgres("postgres")
                       .AddDatabase("catalogdb");

var basketCache = builder.AddRedis("basketcache");

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                     .WithReference(catalogDb)
                     .WithReplicas(2);

var messaging = builder.AddRabbitMQ("messaging");

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
