var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var catalogDb = builder.AddPostgresContainer("postgres").AddDatabase("catalogdb");

var basketCache = builder.AddRedisContainer("basketcache");

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                     .WithReference(catalogDb);

var messaging = builder.AddRabbitMQContainer("messaging");

var basketService = builder.AddProject<Projects.BasketService>("basketservice")
                    .WithReference(basketCache)
                    .WithReference(messaging);

builder.AddProject<Projects.MyFrontend>("frontend")
       .WithReference(basketService)
       .WithReference(catalogService.GetEndpoint("http"))
       .WithEnvironment("TESTVALUE_X", catalogService.GetEndpoint("http"))
       .WithEnvironment(context =>
       {
           if (!catalogService.Resource.TryGetServiceBindings(out var serviceBindings))
           {
               return;
           }

           var binding = serviceBindings.FirstOrDefault(b => b.Name == "http");

           if (binding is null)
           {
               return;
           }

           context.EnvironmentVariables["TESTVALUE_Y"] = binding.UriScheme + "://localhost:" + binding.Port;
       });

builder.AddProject<Projects.OrderProcessor>("orderprocessor")
       .WithReference(messaging)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basketService)
       .WithReference(catalogService);

builder.AddProject<Projects.CatalogDb>("catalogdbapp")
       .WithReference(catalogDb);

builder.Build().Run();
