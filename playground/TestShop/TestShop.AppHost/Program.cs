using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgres("postgres")
                       .WithDataVolume()
                       .WithPgAdmin(resource =>
                       {
                           resource.WithEndpoint("http", e => e.DisplayProperties.DisplayName = "PG Admin");
                       })
                       .AddDatabase("catalogdb");

var basketCache = builder.AddRedis("basketcache")
                         .WithDataVolume();

#if !SKIP_DASHBOARD_REFERENCE
basketCache.WithRedisCommander(c =>
            {
                c.WithHostPort(33801);
                c.WithEndpoint("http", e => e.DisplayProperties.DisplayName = "Redis Commander");
            })
           .WithRedisInsight(c =>
            {
                c.WithHostPort(33802);
                c.WithEndpoint("http", e => e.DisplayProperties.DisplayName = "Redis Insight");
            });
#endif

var catalogDbApp = builder.AddProject<Projects.CatalogDb>("catalogdbapp")
                          .WithReference(catalogDb);

if (builder.Environment.IsDevelopment())
{
    var resetDbKey = Guid.NewGuid().ToString();
    catalogDbApp.WithEnvironment("DatabaseResetKey", resetDbKey)
                .WithHttpCommand("/reset-db", "Reset Database",
                    displayDescription: "Reset the catalog database to its initial state. This will delete and recreate the database.",
                    confirmationMessage: "Are you sure you want to reset the catalog database?",
                    iconName: "DatabaseLightning",
                    configureRequest: requestContext =>
                    {
                        requestContext.Request.Headers.Add("Authorization", $"Key {resetDbKey}");
                        return Task.CompletedTask;
                    });
}

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
       // Add a path & name to all URLs
       .WithUrlPath("/shop", name: "Shopping Page")
       // Add a URL to be displayed in the dashboard
       .WithUrls(c => c.Urls.Add("https://someplace.com", name: "Some place"))
       // Sugar method for adding a URL
       .WithUrl("https://someotherplace.com/some-path", "Some other place")
       // Update all URLs with a generated name
       .WithUrls(c =>
       {
           var i = 1;
           foreach (var url in c.Urls)
           {
               var suffix = url.Url.StartsWith("https://") ? " (secure)" : "";
               url.Name = $"Url {i}{suffix}";
               i++;
           }
       })
       // Hide all non-HTTPS endpoint URLs
       .WithUrls(c =>
       {
           c.Urls.RemoveAll(u => u.Endpoint is not null && !u.Url.StartsWith("https://"));
       })
       // Set host-name for all URLs to the resource name (custom DNS/HOSTS scenario)
       .WithUrls(c =>
       {
           foreach (var url in c.Urls)
           {
               url.Url = (new UriBuilder(url) { Host = c.Resource.Name.ToLowerInvariant() }).ToString();
           }
       })
       //.WithEndpoint("http", c => c.DisplayProperties.DisplayName = $"TestShop UI ({c.UriScheme})")
       //.WithEndpoint("https", c => c.DisplayProperties.DisplayName = $"TestShop UI ({c.UriScheme})")
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
