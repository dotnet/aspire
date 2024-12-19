var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgres("postgres")
                       .WithDataVolume()
                       .WithPgAdmin()
                       .AddDatabase("catalogdb");

var basketCache = builder.AddRedis("basketcache")
                         .WithDataVolume();

#if !SKIP_DASHBOARD_REFERENCE
basketCache.WithRedisCommander(c =>
            {
                c.WithHostPort(33801);
            })
           .WithRedisInsight(c =>
            {
                c.WithHostPort(33802);
            });
#endif

var catalogDbApp = builder.AddProject<Projects.CatalogDb>("catalogdbapp")
                          .WithReference(catalogDb);

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                            .WithReference(catalogDb)
                            .WithReplicas(2);

var messaging = builder.AddRabbitMQ("messaging")
                       .WithDataVolume()
                       .WithLifetime(ContainerLifetime.Persistent)
                       .WithManagementPlugin()
                       .PublishAsContainer();

var basketService = builder.AddProject("basketservice", @"..\BasketService\BasketService.csproj")
                           .WithReference(basketCache)
                           .WithReference(messaging).WaitFor(messaging);

builder.AddProject<Projects.MyFrontend>("frontend")
       .WithExternalHttpEndpoints()
       .WithReference(basketService)
       .WithReference(catalogService);

builder.AddProject<Projects.OrderProcessor>("orderprocessor", launchProfileName: "OrderProcessor")
       .WithReference(messaging).WaitFor(messaging);

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basketService)
       .WithReference(catalogService);

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
