# Aspire.Keycloak library

Add JwtBearer and OpenId Connect authentication to the project via a [Keycloak](https://www.keycloak.org).

## Getting started

### Prerequisites

- A Keycloak server instance
- A Keycloak realm
- For JwtBearer authentication, a configured audience in the Keycloak realm
- For OpenId Connect authentication, the ID of a client configured in the Keycloak realm

### Install the package

Install the .NET Aspire Keycloak library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Keycloak
```

## Jwt bearer authentication usage example

In the _Program.cs_ file of your ASP.NET Core API project, call the `AddKeycloakJwtBearer` extension method to add JwtBearer authentication, using a connection name, realm and any required JWT Bearer options:

```csharp
builder.AddKeycloakJwtBearer("keycloak", realm: "WeatherShop", configureJwtBearerOptions: options =>
{
    options.Audience = "weather.api";
});
```

You can set many other options via the `Action<JwtBearerOptions> configureJwtBearerOptions` delegate.

## OpenId Connect authentication usage example

In the _Program.cs_ file of your Blazor project, call the `AddKeycloakOpenIdConnect` extension method to add OpenId Connect authentication, using a connection name, realm and any required OpenId Connect options:

```csharp
builder.AddKeycloakOpenIdConnect("keycloak", realm: "WeatherShop", configureOpenIdConnectOptions: options =>
{
    options.ClientId = "WeatherWeb";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.Scope.Add("weather:all");
});
```

You can set many other options via the `Action<OpenIdConnectOptions>? configureOpenIdConnectOptions` delegate.

## AppHost extensions

In your AppHost project, install the `Aspire.Hosting.Keycloak` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Keycloak
```

Then, in the _Program.cs_ file of `AppHost`, register a Keycloak server and consume the connection using the following methods:

```csharp
var keycloak = builder.AddKeycloak("keycloak");

var apiService = builder.AddProject<Projects.Keycloak_ApiService>("apiservice")
                        .WithReference(keycloak);

builder.AddProject<Projects.Keycloak_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak)
    .WithReference(apiService);
```

The `WithReference` method configures a connection in the `Keycloak.ApiService` and `Keycloak.Web` projects named `keycloak`.

In the _Program.cs_ file of `Keycloak.ApiService`, the Keycloak connection can be consumed using:

```csharp
builder.AddKeycloakJwtBearer("keycloak", realm: "WeatherShop");
```

And in the _Program.cs_ file of `Keycloak.Web`, the Keycloak connection can be consumed using:

```csharp
builder.AddKeycloakOpenIdConnect("keycloak", realm: "WeatherShop");
```

## Additional documentation

* https://www.keycloak.org/getting-started/getting-started-docker
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire