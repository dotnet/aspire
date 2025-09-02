---
title: What's new in .NET Aspire 9.5
description: Learn what's new in .NET Aspire 9.5.
ms.date: 08/21/2025
---

# What's new in .NET Aspire 9.5

## Table of contents

- [Upgrade to .NET Aspire 9.5](#️-upgrade-to-net-aspire-95)
- [CLI improvements](#-cli-improvements)
- [Dashboard enhancements](#-dashboard-enhancements)
- [Integration changes and additions](#integration-changes-and-additions)
  - [OpenAI hosting integration](#openai-hosting-integration)
- [App model enhancements](#️-app-model-enhancements)
  - [Telemetry configuration APIs](#telemetry-configuration-apis)
  - [Resource waiting patterns](#resource-waiting-patterns)
  - [ExternalService WaitFor behavior change](#externalservice-waitfor-behavior-change)
  - [Context-based endpoint resolution](#context-based-endpoint-resolution)
  - [Resource lifetime behavior](#resource-lifetime-behavior)
  - [InteractionInput API changes](#interactioninput-api-changes)
  - [Custom resource icons](#custom-resource-icons)
  - [MySQL password improvements](#mysql-password-improvements)
  - [Remote & debugging experience](#remote--debugging-experience)
- [Azure](#azure)
  - [Azure AI Foundry enhancements](#azure-ai-foundry-enhancements)
  - [Azure App Configuration emulator](#azure-app-configuration-emulator)
  - [Broader Azure resource capability surfacing](#broader-azure-resource-capability-surfacing)

📢 .NET Aspire 9.5 is the next minor version release of .NET Aspire. It supports:

- .NET 8.0 Long Term Support (LTS)
- .NET 9.0 Standard Term Support (STS)
- .NET 10.0 Preview 6

If you have feedback, questions, or want to contribute to .NET Aspire, collaborate with us on [:::image type="icon" source="../media/github-mark.svg" border="false"::: GitHub](https://github.com/dotnet/aspire) or join us on our new [:::image type="icon" source="../media/discord-icon.svg" border="false"::: Discord](https://aka.ms/aspire-discord) to chat with the team and other community members.

It's important to note that .NET Aspire releases out-of-band from .NET releases. While major versions of Aspire align with major .NET versions, minor versions are released more frequently. For more information on .NET and .NET Aspire version support, see:

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy): Definitions for LTS and STS.
- [.NET Aspire support policy](https://dotnet.microsoft.com/platform/support/policy/aspire): Important unique product lifecycle details.

## ⬆️ Upgrade to .NET Aspire 9.5

Moving between minor releases of Aspire is simple:

1. In your AppHost project file (that is, _MyApp.AppHost.csproj_), update the [📦 Aspire.AppHost.Sdk](https://www.nuget.org/packages/Aspire.AppHost.Sdk) NuGet package to version `9.5.0`:

    ```xml
    <Sdk Name="Aspire.AppHost.Sdk" Version="9.5.0" />
    ```

    For more information, see [.NET Aspire SDK](xref:dotnet/aspire/sdk).

2. Check for any NuGet package updates, either using the NuGet Package Manager in Visual Studio or the **Update NuGet Package** command from C# Dev Kit in VS Code.

3. Update to the latest [.NET Aspire templates](../fundamentals/aspire-sdk-templates.md) by running the following .NET command line:

    ```dotnetcli
    dotnet new install Aspire.ProjectTemplates
    ```

  > [!NOTE]
  > The `dotnet new install` command will update existing Aspire templates to the latest version if they are already installed.

If your AppHost project file doesn't have the `Aspire.AppHost.Sdk` reference, you might still be using .NET Aspire 8. To upgrade to 9, follow [the upgrade guide](../get-started/upgrade-to-aspire-9.md).

## 🚀 CLI improvements

### 🧪 `aspire exec` enhancements

Introduced in 9.4 (behind a feature flag), `aspire exec` in 9.5 adds `--workdir` support plus improved help text and argument validation:

```bash
# Show environment info for an app container
aspire exec --resource api-container -- dotnet --info

# Run a command from a specific working directory (new in 9.5)
aspire exec --resource worker --workdir /app/tools -- dotnet run -- --seed
```

### 🧷 Robust orphan detection

Resilient to PID reuse via `ASPIRE_CLI_STARTED` timestamp + PID verification:

```text
Detected orphaned prior AppHost process (PID reused). Cleaning up...
```

### 📦 Package channel & templating enhancements

New packaging channel infrastructure lets `aspire add` and templating flows surface stable vs pre-release channels with localized UI.

### 📝 Rich markdown rendering

Extended markdown coverage in CLI prompts/output including code fences, emphasis, and safe escaping.

### 🧵 Improved cancellation & CTRL-C UX

- CTRL-C guidance message
- Fix for stall on interrupt

## 🎨 Dashboard enhancements

### 🔗 Deep-linked telemetry navigation

Trace IDs, span IDs, resource names, and log levels become interactive buttons in property grids for one-click navigation between telemetry views (#10648).

### 📊 Multi-resource console logs
 
New "All" option streams logs from every running resource simultaneously with deterministic colored name prefixes and a separate timestamp preference to reduce noise in aggregate view (#10981):

```text
[api      INF] Hosting started
[postgres INF] database system is ready
[redis    INF] Ready to accept connections
```

### 🎭 Custom resource icons

Resources can specify custom icons via `WithIconName()` for better visual identification in dashboard views (#10760).

### 🌐 Reverse proxy support
 
Dashboard now explicitly maps forwarded Host & Proto headers when `ASPIRE_DASHBOARD_FORWARDEDHEADERS_ENABLED=true`, fixing OpenID auth redirects and URL generation behind reverse proxies like YARP (#10388). Only these two headers are allowed to limit spoofing surface:

- Enable with `ASPIRE_DASHBOARD_FORWARDEDHEADERS_ENABLED=true`
- Fixes OpenID authentication redirect issues with proxy scenarios

### 📱 Improved mobile experience

Mobile and desktop toolbars redesigned for better usability across all dashboard pages with improved responsive layouts (#10407).

### 🔧 Enhanced resource management

- Resource action menus reorganized into sub-menus to prevent overflow (#10869)
- LaunchProfile property added to project details for easier debugging (#10906)
- Better resource navigation and selection handling (#10848)

### 🚨 Container runtime notifications

Smart notifications appear when Docker/Podman is installed but unhealthy, with automatic dismissal when runtime recovers (#11008).

### ✨ UI improvements

- Error spans use consistent error styling (#10742)
- Better default icons for parameters and services (#10762)
- Improved navigation reliability (#10848)
- Enhanced port parsing (#10884)
- Message truncation for long log entries (#10882)
- Optional log line wrapping (#10271)
- Improved text visualizer dialog (#10964)

### 📊 Trace performance & integration

- Optimized trace detail page rendering (#10308)
- Embedded log entries within trace spans (#10281)
- Better span timing calculations (#10310)

### 🌍 Localization & deployment

- Comprehensive dashboard localization with consolidated resource files
- Launch profile support with localized display names (#10906)
- Forwarded headers support for proxy/container scenarios (#10388)

## Integration changes and additions

### OpenAI hosting integration

New `AddOpenAI` integration for self-hosted or compatible OpenAI endpoints with child model resources:

```csharp
var openai = builder.AddOpenAI("openai")
  .WithApiKey(builder.AddParameter("OPENAI__API_KEY", secret: true))
  .WithEndpoint("http://localhost:9000");

var chat = openai.AddModel("chat", "gpt-4o-mini");

builder.AddProject<Projects.Api>("api")
  .WithReference(chat);
```

## 🖥️ App model enhancements

### Telemetry configuration APIs

Enhanced OTLP telemetry configuration with protocol selection:

```csharp
// New OtlpProtocol enum with Grpc and HttpProtobuf options
public enum OtlpProtocol
{
    Grpc = 0,
    HttpProtobuf = 1
}

// Configure OTLP telemetry with specific protocol
var api = builder.AddProject<Projects.Api>("api")
  .WithOtlpExporter(OtlpProtocol.HttpProtobuf);

// Or use default protocol
var worker = builder.AddProject<Projects.Worker>("worker")  \
  .WithOtlpExporter();
```

### Resource waiting patterns

Enhanced waiting with new `WaitForStart` options (issue #7532, implemented in PR #10948). `WaitForStart` waits for a dependency to reach the Running state without blocking on health checks—useful when initialization code (migrations, seeding, registry bootstrap) must run before the service can become "healthy". Compared to `WaitFor`:

- `WaitFor` = Running + passes health checks.
- `WaitForStart` = Running only (ignores health checks, faster in dev / init flows).

This mirrors Docker Compose's `service_started` vs `service_healthy` conditions and supports both existing `WaitBehavior` modes.

```csharp
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");

var api = builder.AddProject<Projects.Api>("api")
  .WaitForStart(postgres)  // New: Wait only for startup, not health
  .WaitFor(redis)  // Existing: Wait for healthy
  .WithReference(postgres)
  .WithReference(redis);
```

### ExternalService WaitFor behavior change

Breaking change: `WaitFor` now properly honors `ExternalService` health checks so dependent resources defer start until the external service reports healthy (issue [#10827](https://github.com/dotnet/aspire/issues/10827)). Previously, dependents would start even if the external target failed its readiness probe. This improves correctness and aligns `ExternalService` with other resource types.

If you relied on the old lenient behavior (e.g., starting a frontend while an external API was still warming up), you can temporarily remove the `WaitFor` call or switch to `WaitForStart` if only startup is required.

```csharp
var externalApi = builder.AddExternalService("backend-api", "http://localhost:5082")
  .WithHttpHealthCheck("/health/ready");

builder.AddProject<Projects.Frontend>("frontend")
  .WaitFor(externalApi);
```

### Context-based endpoint resolution

Breaking change: Endpoint resolution in `WithEnvironment` now correctly resolves container hostnames instead of always using "localhost" ([#8574](https://github.com/dotnet/aspire/issues/8574)):

```csharp
var redis = builder.AddRedis("redis");

builder.AddRabbitMQ("rabbitmq")
  .WithEnvironment(context =>
  {
    var endpoint = redis.GetEndpoint("tcp");
    var redisHost = endpoint.Property(EndpointProperty.Host);
    var redisPort = endpoint.Property(EndpointProperty.Port);

    context.EnvironmentVariables["REDIS_HOST"] = redisHost;
    context.EnvironmentVariables["REDIS_PORT"] = redisPort;
  });
```

### Resource lifetime behavior

Breaking change: Several resources now support `WaitFor` operations that were previously not supported ([#10851](https://github.com/dotnet/aspire/pull/10851), [#10842](https://github.com/dotnet/aspire/pull/10842)):

```csharp
var connectionString = builder.AddConnectionString("db");
var apiKey = builder.AddParameter("api-key", secret: true);

builder.AddProject<Projects.Api>("api")
  .WaitFor(connectionString)
  .WaitFor(apiKey);
```

Resources like `ParameterResource`, `ConnectionStringResource`, and GitHub Models no longer implement `IResourceWithoutLifetime`. They now show as "Running" and can be used with `WaitFor` operations.

### InteractionInput API changes

Breaking change: The `InteractionInput` API now requires `Name` and makes `Label` optional ([#10835](https://github.com/dotnet/aspire/pull/10835)):

```csharp
var input = new InteractionInput 
{ 
  Name = "username",
  Label = "Username",
  InputType = InputType.Text 
};
```

All `InteractionInput` instances must now specify a `Name`. The `Label` property is optional and will default to the `Name` if not provided.

### Custom resource icons

Resources can specify custom icon names for better visual identification:

```csharp
var postgres = builder.AddPostgres("postgres")
  .WithIconName("database");

var api = builder.AddProject<Projects.Api>("api")
  .WithIconName("web-app", ApplicationModel.IconVariant.Regular);
```

### MySQL password improvements

Consistent password handling across database resources:

```csharp
var mysql = builder.AddMySql("mysql")
  .WithPassword(builder.AddParameter("mysql-password", secret: true));

// Password can be modified during configuration
mysql.Resource.PasswordParameter = builder.AddParameter("new-password", secret: true);
```

### Remote & debugging experience

### 🔄 SSH remote auto port forwarding

VS Code SSH sessions now get automatic port forwarding configuration just like Dev Containers and Codespaces:

```text
Remote SSH environment detected – configuring forwarded ports (dashboard, api, postgres).
```

### 🐞 AppHost debugging in VS Code

The extension offers Run vs Debug for the AppHost. If the C# extension is present it launches under the debugger; otherwise a terminal with `dotnet watch` is used.

### 🧩 Extension modernization

Package upgrades and localization support plus groundwork for richer debugging scenarios.

## Azure

### Azure AI Foundry enhancements

9.5 adds a generated, strongly-typed model catalog (`AIFoundryModel`) for IntelliSense + ref safety when creating deployments (PR #10986) and a daily automation that refreshes the catalog as new models appear in Azure AI Foundry (PR #11040). Sample apps and end-to-end tests now use these constants (PR #11039) instead of raw strings. The original Foundry hosting integration and local runtime support were introduced earlier (issue #9568); this release focuses on developer ergonomics and keeping model metadata current.

Strongly-typed model catalog with IntelliSense support:

```csharp
var aiFoundry = builder.AddAzureAIFoundry("ai-foundry");

// Strongly-typed model references
var gpt4 = aiFoundry.AddDeployment("gpt-4", AIFoundryModel.OpenAI.Gpt4);
var mistral = aiFoundry.AddDeployment("mistral", AIFoundryModel.MistralAi.MistralLarge2411);

// Local on-device mode
var localFoundry = builder.AddAzureAIFoundry("local-ai")
  .RunAsFoundryLocal();
```

### Azure App Configuration emulator

Run emulators locally with full configuration support:

```csharp
var appConfig = builder.AddAzureAppConfiguration("config")
  .RunAsEmulator(emulator => emulator
    .WithDataVolume("config-data")
    .WithHostPort(8080));
```

### Broader Azure resource capability surfacing

Several Azure hosting resource types now implement `IResourceWithEndpoints` enabling uniform endpoint discovery and waiting semantics:

- `AzureAIFoundryResource`
- `AzureAppConfigurationResource`  
- `AzureKeyVaultResource`
- `AzurePostgresFlexibleServerResource`
- `AzureRedisCacheResource`

