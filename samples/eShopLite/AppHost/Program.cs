var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

/// Goal: Add a prometheus and grafana container to the eShopLite sample
///
/// builder.AddProject<Projects.MyFrontend>("frontend")
///
/// builder.AddProject<Projects.OrderProcessor>("orderprocessor")
///
/// builder.AddProject<Projects.ApiGateway>("apigateway")
///
/// builder.AddProject<Projects.CatalogDb>("catalogdbapp")
///
/// var prometheus = builder.AddPrometheusContainer("prometheus", "prometheus.yml", "prometheus-data")
///                         .Scrape(builder.GetProject("frontend"))
///                         .Scrape(builder.GetProject("orderprocessor"))
///                         ...
///                         
/// var grafana = builder.AddGrafanaContainer("grafana", "grafana.ini", "grafana-data")
///                      .WithDashboard("eShopLite.json")
///                      .WithDashboard("eShopLite2.json")
///                      .AddDataSource("prometheus", "http://prometheus:9090")
///                      // or .AddDataSource(prometheus)
///                      ...
/// 
///
/// builder.Build().Run();

var catalogDb = builder.AddPostgresContainer("postgres").AddDatabase("catalogdb");

var basketCache = builder.AddRedisContainer("basketcache");

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                     .WithReference(catalogDb)
                     .WithReplicas(2);

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
       .WithReference(catalogDb);

builder.Build().Run();
