var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddKeycloak("keycloak");
var realm = "WeatherShop";

var apiService = builder.AddProject<Projects.Keycloak_ApiService>("apiservice")
                        .WithReference(keycloak, realm);

builder.AddProject<Projects.Keycloak_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak, realm)
    .WithReference(apiService);

builder.Build().Run();
