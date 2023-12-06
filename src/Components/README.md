# Overview

Aspire components are classic .NET NuGet packages which are designed as highly usable libraries. .NET Aspire components feature rich production-ready telemetry, health checks, configurability, testability, and documentation. For the current state of the components included in this repo and tracked for .NET Aspire's first preview, please check out the [.NET Aspire Components Progress](./Aspire_Components_Progress.md) page.

## Contribution guidelines

We aim to have a diverse set of high quality Aspire components, making it easy to pick from many different technologies when building Aspire apps. We expect to continue to add more components, and we welcome contributions of others, but we explicitly don't want to include every possible component. The set will be gently curated: in order to make sure that components are useful and dependable, we have some broad criteria below for components contributed to dotnet/aspire. These will likely evolve over time based on feedback, but we expect some requirements (such as actively supported) to remain firm:

1. We expect to welcome any components that would have value to Aspire users and align with what Aspire is intended to do, subject to the below.
2. We don't expect to choose preferred techs. For example, if there are two commonly used providers for database XYZ, we are comfortable with having one component for each. We would like component naming and granularity to be clear enough that customers can make informed decisions. Aspire is agnostic to your choice of cloud provider, too.
3. We will require that the tech represented by the component is being actively supported. In most cases we expect that it is widely used, although we expect that part will be a judgement call.
4. Components contributed to dotnet/aspire must meet the same quality and completeness bar of other contributions. ie., we won't have a lower quality bar for experimental or niche components.
5. Where there's a component that meets the above criteria, but that isn't something we expect to be a high priority for the Aspire committers to maintain, we'll ask for a plan to sustain it (eg., motivated contributors ready to fix bugs in it)

Note: only components that are built from dotnet/aspire will be able to use the Aspire package name prefix. There is no technical barrier to using components built elsewhere, without the Aspire prefix, but currently our idea is that all broadly available Aspire components will live here in dotnet/aspire and have the Aspire package prefix. We welcome feedback on this and all the other principles listed here, though.

In summary we encourage and are excited to accept contributions of components, but it's probably a good idea to first open an issue to discuss any new potential component before offering a PR, to make sure we're all in agreement that it's a good fit with these principles.

## Versioning and Releases

Each component is in its own NuGet package, and can version independently, including declaring itself in a preview state using the standard SemVer and NuGet mechanisms. However we expect the major and minor version of components to follow the core Aspire packages to make it easier to reason about dependencies. We expect to typically push updates to all components at the same time we update the core Aspire packages, but we have the ability to push an updated component at any other time if necessary, for example where changes to the underlying client library makes it necessary.

## Icon

Where the component represents some client technology that has a widely recognized logo, we would like to use that for the package icon if we can. Take a look at the MySql component for an example. We can only do this if the owner of the logo allows it - often you can find posted guidelines describing acceptable usage. Otherwise we can add reach out for explicit permission and do a follow up commit to add the icon if and when the use is approved.

## Naming

- Each component's name must have the prefix `Aspire.`.
- When component is built around `ABC` client library, it should contain the client library name in its name. Example: `Aspire.ABC`. Where the technology has a particular casing we have preferred that: for example `Aspire.RabbitMQ` rather than `Aspire.RabbitMq`.
- When the client library is just one of many libraries that allows to consume given service, the names that refer to component need to be specific, not generic. Example: Npgsql is not the only db driver for PostgreSQL database, so the extension method should be called `AddNpgsql` rather than `AddPostgreSQL`. The goal here is to help app authors make an informed decision about which to choose.

## Public API

- Each component should have an `AddXXX` extension method extending `IHostApplicationBuilder`
    - Consider adding support for [keyed DI](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8#keyed-di-services), if applicable.
    - A component can have more APIs, if necessary, but it is an explicit goal of Aspire Components to **not** wrap the underlying client library's APIs in new, convenient, higher-level APIs.
- Each component should provide it's own public and `sealed` `Settings` type.
  > [!NOTE]
  > This type does not use the name `Options` because it is not an [IOptions](https://learn.microsoft.com/dotnet/core/extensions/options). `IOptions` objects can be configured through dependency injection. These settings need to be read before the DI container is built, so they can't be `IOptions`.
- The settings type name should be unique (no generic names like `ConfigurationOptions`), not contain an `Aspire` prefix and follow the client-lib name. Example: when a component wraps an `ABC` client library, the package is called `Aspire.ABC` and the settings type is named `ABCSettings` and is either in the `Aspire.ABC` namespace or a sub namespace.

## Configuration

- When a new instance of the settings type is created, its properties should return the recommended/default values (so when they are bound to an empty config they still return the right values).
- Settings should be bound to a section of `IConfiguration` exposed by `IHostApplicationBuilder.Configuration`.
- Each component should determine a constant configuration section name for its settings under the `Aspire` config section.
- All configuration knobs exposed by the settings type should be public and mutable, so they can be changed in the config and applied without a need for re-compiling the application.
- Each component should expose an optional lambda that accepts an instance of given settings type. By doing that, we provide the users with a possibility to override the bound config values (make final changes).
- When a mandatory config property is missing, an exception should be thrown, and it should contain information about the config path that was used to read it.

```csharp
public static void AddAbc(this IHostApplicationBuilder builder, Action<AbcSettings>? configureSettings = null)
{
    ArgumentNullException.ThrowIfNull(builder);

    var settings = new AbcSettings();
    builder.Configuration.GetSection("Aspire:Abc").Bind(settings);

    configureSettings?.Invoke(settings);

    if (settings.MandatoryPropertyIsMissing)
        throw new MeaningfulException($"MandatoryPropertyName was not found at configuration path 'Aspire:Abc:{nameof(AbcSettings.MandatoryPropertyName)}'");
}
```

### Named Configuration

- Components should allow for multiple (named) instances to be registered in the application.
- The configuration for each named instance will come from a section under the component's section with the name corresponding to the name provided when the component was registered.

```json
{
  "Aspire": {
    "Abc": {
      "named_one": {

      },
      "named_two": {

      }
    }
  }
}
```

## Health Checks

Aspire components expose health checks enabling applications to track and respond to the remote service’s health.

- Health checks should be enabled by default, but the users should be able to disable them via configuration.
- [AddHealthChecks(this IServiceCollection)](https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.healthcheckservicecollectionextensions) should be used to register health checks.
- If the client library provides an integration with `HealthCheckService` (example: [Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks#entity-framework-core-dbcontext-probe)) it should be used.
- If there is an established open-source health check (example: [AspNetCore.HealthChecks.Redis](https://www.nuget.org/packages/AspNetCore.HealthChecks.Redis)) it should be used. If the existing health check library doesn't meet our requirements, efforts should be made to add the necessary functionality to the existing library.
- Otherwise we need to implement [IHealthCheck](https://learn.microsoft.com/dotnet/api/microsoft.extensions.diagnostics.healthchecks.ihealthcheck) and register it via [HealthCheckRegistration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.diagnostics.healthchecks.healthcheckregistration).
- Consider whether the Health Check should reuse the same client object registered in DI by the component or not. Reusing the same client object has the advantages of getting the same configuration, logging, etc during the health check.
- Calling [MapHealthChecks](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.healthcheckendpointroutebuilderextensions.maphealthchecks) is outside of the scope of a Component.

## Resilience

Aspire components leverage configurable resilience patterns such as retries, timeouts, and circuit breakers to maximize availability. This functionality is configurable and seamlessly integrates with higher level resilience strategies implemented at the application level.

- Each component must ensure that by default reasonable timeouts are enabled. It should be possible to configure the timeouts.
- If the client library provides connection pooling, it should be enabled by default (to scale proportionally). It should be possible to disable it via configuration.
- If given client library provides built in mechanism for retries, it should be enabled by default and configurable.
- It's not always possible to implement retries. Example: raw db driver does not know, whether currently executed command is part of a transaction or not. If it is, re-trying a failed command won't help as the whole transaction has already failed.

## Telemetry

Aspire components offer integrated logging, metrics, and tracing using modern .NET abstractions (ILogger, Meter, Activity). Telemetry is schematized and part of a component’s contract, ensuring backward compatibility across versions of the component.

- The Component's telemetry names should conform to [OpenTelemetry's Semantic Conventions](https://github.com/open-telemetry/semantic-conventions) when available.
- Components are allowed to use OpenTelemetry [Instrumentation Libraries](https://opentelemetry.io/docs/specs/otel/glossary/#instrumentation-library), if available. (example: [OpenTelemetry.Instrumentation.StackExchangeRedis](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.StackExchangeRedis)).
- The Component should use `ILoggerFactory`/`ILogger` objects that come from DI.
- If possible, no information should be logged twice (example: raw db driver and Entity Framework can both log SQL queries, when they are used together only one should be logging).
- Defining [telemetry exporters](https://opentelemetry.io/docs/instrumentation/net/exporters/) is outside of the scope of a Component.

## Performance

- Entity Framework DbContext pooling should be enabled by default, but it should be possible to disable it (example: multi-tenant application where `DbContext` may contain data specific to customer and should not be reused for other customers).
- Registering a `DbDataSource` and obtaining a `DbConnection` from it should be preferred over instantiating `DbConnection` directly with a connection string.
- Before every component is shipped, we should measure how applying the recommended settings affect performance.

## Azure Components

### Configuration

- Azure SDK libraries already have a `ClientOptions` type that is an `IOptions`. These options provide general HTTP options like `RetryOptions` and `DiagnosticsOptions`, but also service-specific options, like `ServiceBusClientOptions.ConnectionIdleTimeout`.
- Since users will need to be able to configure both `Aspire` settings and these `ClientOptions` options, we will nest the `ClientOptions` configuration under the components configuration section.

```json
{
  "Aspire": {
    "Azure": {
      "Messaging:ServiceBus": {
        // Aspire settings
        "HealthChecks": false,
        "Namespace": "aspire-servicebus.servicebus.windows.net",

        // Azure SDK's ServiceBusClientOptions
        "ClientOptions": {
          "RetryOptions": {
            "MaxRetries": 2,
            "Delay": "00:00:01"
          }
        }
      }
    }
  }
}
```

### Security

- If the underlying client library supports passwordless/[RBAC](https://learn.microsoft.com/azure/role-based-access-control/overview) authentication, which Credential to use should be configurable through the .NET Aspire Settings object. For example:

```csharp
builder.AddAzureServiceBus(settings =>
{
    settings.Credential =
        new ChainedTokenCredential(
            new VisualStudioCredential(),
            new VisualStudioCodeCredential());
});
```

- If the underlying client library supports secret credentials (like a connection string), this should be read from `IConfiguration`. This can be placed either in the component-specific section, or under the global `ConnectionStrings` section. If both are specified, the component-specific section is used. For example:

```json
{
  "ConnectionStrings": {
    "Aspire.Azure.Messaging.ServiceBus": "Endpoint=sb://foo;..."
  },
  "Aspire": {
    "Azure": {
      "Messaging:ServiceBus": {
        "ConnectionString": "Endpoint=sb://foo;..."
      }
    }
  }
}
```

- Alternatively, the ConnectionString should be able to be configured through the .NET Aspire Settings object. For example:

```csharp
builder.AddAzureServiceBus(settings =>
{
    settings.ConnectionString = "xyz";
});
```

- If both secret and passwordless mechanisms are configured, the secret credential overrides the passwordless identity setting.

## Checklist for new components

New components MUST have:

* README.md
* ConfigurationSchema.json file
* Public APIs
* Tests
    * [ConformanceTests](../../tests/Aspire.Components.Common.Tests/ConformanceTests.cs)
    * Other unit tests as needed
* Tracing support

New components SHOULD* have:

* Logging support
* Metrics support
* Health checks

`*` Components need to have justification for why these are not supported.
