# Aspire.Hosting.Docker library

Provides publishing extensions to Aspire for Docker Compose.

## Getting started

### Install the package

In your AppHost project, install the Aspire Docker Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package Aspire.Hosting.Docker
```

## Usage examples

### Publishing to Docker Compose

To publish an Aspire application to Docker Compose, add the Docker Compose environment in the _AppHost.cs_ file:

```csharp
builder.AddDockerComposeEnvironment("compose");
```

Then publish using the Aspire CLI:

```shell
aspire publish -o docker-compose-artifacts
```

### Importing from Docker Compose

You can import existing Docker Compose files into your Aspire application model using `AddDockerComposeFile`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Import services from a docker-compose.yml file
builder.AddDockerComposeFile("myservices", "./docker-compose.yml");

builder.Build().Run();
```

This will parse the Docker Compose file and create Aspire container resources for each service that has an `image` specified. The following Docker Compose features are supported:

- **Image**: Container image name and tag
- **Ports**: Port mappings (mapped to Aspire endpoints)
- **Environment**: Environment variables
- **Volumes**: Both bind mounts and named volumes
- **Command**: Container command arguments
- **Entrypoint**: Container entrypoint
- **Build**: Services with build configurations are imported using `AddDockerfile`
- **Depends On**: Service dependencies are mapped to `WaitFor`, `WaitForStart`, or `WaitForCompletion` based on the condition

**Note**: Other Docker Compose features like networks, health checks, and restart policies are not automatically imported but can be configured manually on the created resources.

## Feedback & contributing

https://github.com/dotnet/aspire
