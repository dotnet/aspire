# Manifest Specification for Aspire's Distributed Application Model

This is a sepcification for the manifest file for Aspire's Distributed Application Model. The purpose of the manifest file is to allow developers to export definitions of components that comprise their distributed application model and their dependencies so that other tools can process it to facilitate deployment into target runtime environments.

The format of the manifest file itself does not pre-suppose a particular target environment but this document will make reference to specific cloud providers and technologies for illustrative purposes.

## Basic model

The Aspire distributed application model is comprised components which are typically deployed together as a unit. For example there may be a front-end ASP.NET Core application which calls into one or more backend services which in turn may depend on relational databases or caches. Consider the following sample:

```csharp
var builder = DistributedApplication.Create(args);

var redis = builder.AddRedisContainer("myredis");
var postgres = builder.AddPostgresContainer("mypostgres");

var backend = builder.AddProject<Backend>("backend")
                     .WithPostgresDatabase(postgres, databaseName: "catalog");

var frontend = builder.AddProject<Frontend>("frontend")
                      .WithRedis(redis)
                      .WithServiceReference(backend, "http");

buidler.Build().Run();
```

When ```dotnet publish``` is called on the DevHost project containing the code above the application model and dependency projects will be built and the devhost will be executed in a model which emits an ```aspire-manifest.json``` file in the build artifacts for the DevHost project. The manifest file for the above project would look like the following (WIP -- annotated with comments):

```json
{
    "$schema": "<url ot stable schema version",

    // This is where the bulk of the manifest is:
    "components": { // An object instead of an array to make traversal easy in scripts etc.

        "frontend": {

            // Aligns with the frontend project above but by the time it gets to the manifest
            // we should have sufficient detail to say its a container and where to find the bits
            // for the container.
            "type": "container.v1", // Types are versioned explicitly ...
                                    // if a processing tool does not understand
                                    // a type anywhere in the document, it
                                    // should abort before taking any action.
            "image": {
                // Option A
                "name": "centralregistry.azurecr.io/frontend@sha256:....",
                "registry": "centralregistry.azurecr.io",
                "repository": "frontend",
                "digest": "sha256:....",
                "tag": "1.0.1-ci.20230918.1",

                // Option B
                "path": "manifest/relative/path/to/container.tgz"
            },

            // Information to support orchestrator on how to reach this container.
            "bindings": {
                "http": {
                    "containerPort": 3000,
                    "scheme": "http",
                }
            },

            "referencedServices": {
                "backend": {
                    "binding": "private-endpoint"
                }
            },

            // Environment variables that need to be treated like secrets.
            "secrets": {
                "Aspire.StackExchange.Redis__ConfigurationOptions__EndPoints__0": "{myredis.connectionString}",
            }

        },

        "backend": {

            "type": "container.v1",

            "image": {
                "name": "centralregistry.azurecr.io/backend@sha256:....",
                "registry": "centralregistry.azurecr.io",
                "repository": "backend",
                "digest": "sha256:....",
                "tag": "1.0.1-ci.20230918.1"
            },

            "bindings": {
                "private-endpoint": {
                    "containerPort": 3000,
                    "scheme": "https",
                }
            },

            "secrets": {
                "ConnectionStrings__Aspire.PostgreSQL": "{mypostgres.connectionString}"
            }

        },

        // Aspire has a number of first class types for common cloud dependencies. It is the responsibility of
        // of the tool to translate this to the most appropriate cloud resource. For example the tool may
        // based on user preferences may provision a springboard redis instance, or a managed redis instance,
        // or it may just be another container in the orchestrator.
        "myredis": {

            "type": "redis.v1"
        },

        "mypostgres": {

            "type": "postgres.v1",

        }
    }
}
```
