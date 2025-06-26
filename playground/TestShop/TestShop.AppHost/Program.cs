using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.AddPostgres("postgres")
                       .WithDataVolume()
                       .WithPgAdmin(resource =>
                       {
                           resource.WithUrlForEndpoint("http", u => u.DisplayText = "PG Admin");
                       })
                       .AddDatabase("catalogdb");

var basketCache = builder.AddRedis("basketcache")
                         .WithDataVolume();

#if !SKIP_DASHBOARD_REFERENCE
basketCache.WithRedisCommander(c =>
            {
                c.WithHostPort(33801);
                c.WithUrlForEndpoint("http", u => u.DisplayText = "Redis Commander");
            })
           .WithRedisInsight(c =>
            {
                c.WithHostPort(33802);
                c.WithUrlForEndpoint("http", u => u.DisplayText = "Redis Insight");
            });
#endif

var catalogDbApp = builder.AddProject<Projects.CatalogDb>("catalogdbapp")
                          .WithReference(catalogDb);

if (builder.Environment.IsDevelopment() && builder.ExecutionContext.IsRunMode)
{
    var resetDbKey = Guid.NewGuid().ToString();
    catalogDbApp.WithEnvironment("DatabaseResetKey", resetDbKey)
                .WithHttpCommand("/reset-db", "Reset Database",
                    commandOptions: new()
                    {
                        Description = "Reset the catalog database to its initial state. This will delete and recreate the database.",
                        ConfirmationMessage = "Are you sure you want to reset the catalog database?",
                        IconName = "DatabaseLightning",
                        PrepareRequest = requestContext =>
                        {
                            requestContext.Request.Headers.Add("Authorization", $"Key {resetDbKey}");
                            return Task.CompletedTask;
                        }
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

var frontend = builder.AddProject<Projects.MyFrontend>("frontend")
       .WithExternalHttpEndpoints()
       .WithReference(basketService)
       .WithReference(catalogService)
       // Modify the display text of the URLs
       .WithUrls(c => c.Urls.ForEach(u => u.DisplayText = $"Online store ({u.Endpoint?.EndpointName})"))
       // Don't show the non-HTTPS link on the resources page (details only)
       .WithUrlForEndpoint("http", url => url.DisplayLocation = UrlDisplayLocation.DetailsOnly)
       // Add health relative URL (show in details only)
       .WithUrlForEndpoint("https", ep => new() { Url = "/health", DisplayText = "Health", DisplayLocation = UrlDisplayLocation.DetailsOnly })
       .WithHttpHealthCheck("/health");

builder.AddProject<Projects.OrderProcessor>("orderprocessor", launchProfileName: "OrderProcessor")
       .WithReference(messaging).WaitFor(messaging);

builder.AddYarp("apigateway")
       .WithConfigFile("yarp.json")
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
