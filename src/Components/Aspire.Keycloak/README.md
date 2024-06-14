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

In the _Program.cs_ file of your ASP.NET Core API project, call the `AddKeycloakJwtBearer` extension method to add JwtBearer authentication. The method takes a connection name parameter.

```csharp
builder.AddKeycloakJwtBearer("keycloak", configureJwtBearerOptions: options =>
{
    options.Audience = "weather.api";
});
```

## Jwt bearer authentication configuration

The .NET Aspire Keycloak component provides multiple options to configure the server connection based on the requirements and conventions of your project.

### Use configuration providers

The .NET Aspire Keycloak component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `KeycloakSettings` from configuration by using the `Aspire:Keycloak` key. Example `appsettings.json` that configures the Keycloak endpoint:
```json
{
  "Aspire": {
    "Keycloak": {
      "Realm": "WeatherShop"
    }
  }
}
```

### Use inline delegates

Also you can pass the `Action<KeycloakSettings> configureSettings` delegate to set up some or all the options inline, for example to set the Realm and Audience from code:

```csharp
builder.AddKeycloakJwtBearer("keycloak", configureSettings: settings =>
{
    settings.Realm = "WeatherShop";
},
configureJwtBearerOptions: options =>
{
    options.Audience = "weather.api";
});
```

## OpenId Connect authentication usage example

In the _Program.cs_ file of your Blazor project, call the `AddKeycloakOpenIdConnect` extension method to add OpenId Connect authentication. The method takes a connection name parameter.

```csharp
builder.AddKeycloakOpenIdConnect("keycloak", configureOpenIdConnectOptions: options =>
{
    options.ClientId = "WeatherWeb";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.Scope.Add("weather:all");
});
```

## OpenId Connect authentication configuration

The .NET Aspire Keycloak component provides multiple options to configure the server connection based on the requirements and conventions of your project.

### Use configuration providers

The .NET Aspire Keycloak component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `KeycloakSettings` from configuration by using the `Aspire:Keycloak` key. Example `appsettings.json` that configures some of the options:
```json
{
  "Aspire": {
    "Keycloak": {
      "Realm": "WeatherShop"
    }
  }
}
```

### Use inline delegates

Also you can pass the `Action<KeycloakSettings> configureSettings` delegate to set up some or all the options inline, for example to set the Realm and ClientId from code:

```csharp
builder.AddKeycloakOpenIdConnect("keycloak", configureSettings: settings =>
{
    settings.Realm = "WeatherShop";
},
configureOpenIdConnectOptions: options =>
{
    options.ClientId = "WeatherWeb";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.Scope.Add("weather:all");
});
```

## AppHost extensions

In your AppHost project, install the `Aspire.Hosting.Keycloak` library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Keycloak
```

Then, in the _Program.cs_ file of `AppHost`, register a Keycloak server and consume the connection using the following methods:

```csharp
var keycloak = builder.AddKeycloak("keycloak");
var realm = "WeatherShop";

var apiService = builder.AddProject<Projects.Keycloak_ApiService>("apiservice")
                        .WithReference(keycloak, realm);

builder.AddProject<Projects.Keycloak_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(keycloak, realm)
    .WithReference(apiService);
```

The `WithReference` method configures a connection in the `Keycloak.ApiService` and `Keycloak.Web` projects named `keycloak` and also sets the `Aspire__Keycloak__Endpoint` environment variable in both projects to the Keycloak realm URL for the `WeatherShop` realm (like http://localhost:63164/realms/WeatherShop).

In the _Program.cs_ file of `Keycloak.ApiService`, the Keycloak connection can be consumed using:

```csharp
builder.AddKeycloakJwtBearer("keycloak");
```

And in the _Program.cs_ file of `Keycloak.Web`, the Keycloak connection can be consumed using:

```csharp
builder.AddKeycloakOpenIdConnect("keycloak");
```

## Additional documentation

* https://www.keycloak.org/getting-started/getting-started-docker
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire