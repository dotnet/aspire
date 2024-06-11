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
builder.AddKeycloakJwtBearer("keycloak");
```

## Jwt bearer authentication configuration

The .NET Aspire Keycloak component provides multiple options to configure the server connection based on the requirements and conventions of your project.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddKeycloakJwtBearer()`:

```csharp
builder.AddKeycloakJwtBearer("keycloak");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "keycloak": "Endpoint=http://localhost:8080"
  }
}
```

### Use configuration providers

The .NET Aspire Keycloak component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `KeycloakSettings` from configuration by using the `Aspire:Keycloak` key. Example `appsettings.json` that configures some of the options:
```json
{
  "Aspire": {
    "Keycloak": {
      "Realm": "gameshop",
      "Audience": "catalog.api"
    }
  }
}
```

### Use inline delegates

Also you can pass the `Action<KeycloakSettings> configureSettings` delegate to set up some or all the options inline, for example to set the Realm and Audience from code:

```csharp
builder.AddKeycloakJwtBearer("keycloak", configureSettings: settings =>
{
    settings.Realm = "gameshop";
    settings.Audience = "catalog.api";    
});
```

## OpenId Connect authentication usage example

In the _Program.cs_ file of your Blazor project, call the `AddKeycloakOpenIdConnect` extension method to add OpenId Connect authentication. The method takes a connection name parameter.

```csharp
builder.AddKeycloakOpenIdConnect("keycloak");
```

## OpenId Connect authentication configuration

The .NET Aspire Keycloak component provides multiple options to configure the server connection based on the requirements and conventions of your project.

### Use a connection string

When using a connection string from the `ConnectionStrings` configuration section, you can provide the name of the connection string when calling `builder.AddKeycloakOpenIdConnect()`:

```csharp
builder.AddKeycloakOpenIdConnect("keycloak");
```

And then the connection string will be retrieved from the `ConnectionStrings` configuration section:

```json
{
  "ConnectionStrings": {
    "keycloak": "Endpoint=http://localhost:8080"
  }
}
```

### Use configuration providers

The .NET Aspire Keycloak component supports [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration). It loads the `KeycloakSettings` from configuration by using the `Aspire:Keycloak` key. Example `appsettings.json` that configures some of the options:
```json
{
  "Aspire": {
    "Keycloak": {
      "Realm": "gameshop",
      "ClientId": "Frontend"
    }
  }
}
```

### Use inline delegates

Also you can pass the `Action<KeycloakSettings> configureSettings` delegate to set up some or all the options inline, for example to set the Realm and ClientId from code:

```csharp
builder.AddKeycloakOpenIdConnect("keycloak", configureSettings: settings => {
    settings.Realm = "gameshop";
    settings.ClientId = "Frontend";
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

var catalogApi = builder.AddProject<Projects.Catalog_Api>("catalog-api")
                        .WithReference(keycloak);

builder.AddProject<Projects.GameShop_Frontend>("gameshop-frontend")
       .WithReference(catalogApi)
       .WithReference(keycloak);
```

The `WithReference` method configures a connection in the `Catalog.Api` and `GameShop.Frontend` projects named `keycloak`. In the _Program.cs_ file of `Catalog.Api`, the Keycloak connection can be consumed using:

```csharp
builder.AddKeycloakJwtBearer("keycloak");
```

And in the _Program.cs_ file of `GameShop.Frontend`, the Keycloak connection can be consumed using:

```csharp
builder.AddKeycloakOpenIdConnect("keycloak");
```

## Additional documentation

* https://www.keycloak.org/getting-started/getting-started-docker
* https://github.com/dotnet/aspire/tree/main/src/Components/README.md

## Feedback & contributing

https://github.com/dotnet/aspire

