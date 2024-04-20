var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgres("postgres")
                       .WithPgAdmin()
                       .AddDatabase("catalogdb");

//var basketCache = builder.AddRedis("basketcache")
//                         .WithDataVolume()
//                         .WithRedisCommander(c =>
//                         {
//                             c.WithHostPort(33801);
//                         });

var basketCache = builder.AddValkey("basketcache")
                         .WithDataVolume();

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                            .WithReference(catalogDb)
                            .WithReplicas(2);

var messaging = builder.AddRabbitMQ("messaging", password: builder.CreateStablePassword("rabbitmq-password", special: false))
                       .WithDataVolume()
                       .WithManagementPlugin()
                       .PublishAsContainer();

var basketService = builder.AddProject("basketservice", @"..\BasketService\BasketService.csproj")
                           .WithReference(basketCache)
                           .WithReference(messaging);

builder.AddProject<Projects.MyFrontend>("frontend")
       .WithExternalHttpEndpoints()
       .WithReference(basketService)
       .WithReference(catalogService);

builder.AddProject<Projects.OrderProcessor>("orderprocessor", launchProfileName: "OrderProcessor")
       .WithReference(messaging);

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basketService)
       .WithReference(catalogService);

builder.AddProject<Projects.CatalogDb>("catalogdbapp")
       .WithReference(catalogDb);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
