using Aspire.Hosting.Azure;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Redis;
using Aspire.Hosting.SqlServer;
using Projects = DevHost.Projects;

var builder = DistributedApplication.CreateBuilder(args);

var grafana = builder.AddContainer("grafana", "grafana/grafana")
       .WithServiceBinding(containerPort: 3000, name: "grafana-http", scheme: "http");

var postgres = builder.AddPostgresContainer("postgres");
var redis = builder.AddRedisContainer("redis");
var sql = builder.AddSqlServerContainer("sql");

var catalog = builder.AddProject<Projects.CatalogService>()
                     .WithPostgresDatabase(postgres, databaseName: "catalogdb")
                     .WithReplicas(2)
                     .WithSqlServer(sql, "master");

var serviceBus = builder.AddAzureServiceBus("messaging");

var basket = builder.AddProject<Projects.BasketService>()
                    .WithRedis(redis)
                    .WithAzureServiceBus(serviceBus);

builder.AddProject<Projects.MyFrontend>()
       .WithServiceReference(basket)
       .WithServiceReference(catalog, bindingName: "http")
       .WithEnvironment("GRAFANA_URL", () => grafana.GetEndpoint("grafana-http").UriString);

builder.AddProject<Projects.OrderProcessor>()
       .WithAzureServiceBus(serviceBus)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>()
       .WithServiceReference(basket)
       .WithServiceReference(catalog);

builder.AddContainer("prometheus", "prom/prometheus")
       .WithVolumeMount("../prometheus", "/etc/prometheus")
       .WithServiceBinding(9090);

builder.Build().Run();
