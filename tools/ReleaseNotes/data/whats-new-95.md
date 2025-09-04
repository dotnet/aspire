---
title: What's new in .NET Aspire 9.5
description: Learn what's new in .NET Aspire 9.5.
ms.date: 08/21/2025
---

# What's new in .NET Aspire 9.5

## Table of contents

- [Upgrade to .NET Aspire 9.5](#upgrade-to-net-aspire-95)
- [CLI improvements](#cli-improvements)
  - [`aspire exec` command enhancements](#aspire-exec-command-enhancements)
  - [Robust orphan detection](#robust-orphan-detection)
  - [Package channel & templating enhancements](#package-channel--templating-enhancements)
  - [Improved cancellation & CTRL-C UX](#improved-cancellation--ctrl-c-ux)
  - [New aspire update command (preview)](#new-aspire-update-command-preview)
  - [Channel-aware aspire add & templating](#channel-aware-aspire-add--templating)
  - [Smarter package prefetching](#smarter-package-prefetching)
  - [Orphan & runtime diagnostics](#orphan--runtime-diagnostics)
  - [Localization & resource strings](#localization--resource-strings)
  - [Developer ergonomics](#developer-ergonomics)
  - [Command infrastructure & interactions](#command-infrastructure--interactions)
  - [Stability & error handling](#stability--error-handling)
- [Dashboard enhancements](#dashboard-enhancements)
  - [Deep-linked telemetry navigation](#deep-linked-telemetry-navigation)
  - [Multi-resource console logs](#multi-resource-console-logs)
  - [Custom resource icons](#custom-resource-icons)
  - [Reverse proxy support](#reverse-proxy-support)
  - [Improved mobile experience](#improved-mobile-experience)
  - [Enhanced resource management](#enhanced-resource-management)
  - [Container runtime notifications](#container-runtime-notifications)
  - [UI improvements](#ui-improvements)
  - [Trace performance & integration](#trace-performance--integration)
  - [Localization & deployment](#localization--deployment)
- [Integration changes and additions](#integration-changes-and-additions)
  - [OpenAI hosting integration](#openai-hosting-integration)
  - [GitHub Models typed catalog](#github-models-typed-catalog)
- [App model enhancements](#app-model-enhancements)
  - [Telemetry configuration APIs](#telemetry-configuration-apis)
  - [Resource waiting patterns](#resource-waiting-patterns)
  - [ExternalService WaitFor behavior change](#externalservice-waitfor-behavior-change)
  - [Context-based endpoint resolution](#context-based-endpoint-resolution)
  - [Resource lifetime behavior](#resource-lifetime-behavior)
  - [InteractionInput API changes](#interactioninput-api-changes)
  - [MySQL password improvements](#mysql-password-improvements)
  - [Remote & debugging experience](#remote--debugging-experience)
- [Azure](#azure)
  - [Azure AI Foundry enhancements](#azure-ai-foundry-enhancements)
  - [Azure App Configuration emulator APIs](#azure-app-configuration-emulator-apis)
  - [Azure Storage emulator improvements](#azure-storage-emulator-improvements)
  - [Broader Azure resource capability surfacing](#broader-azure-resource-capability-surfacing)
  - [Azure resource reference properties](#azure-resource-reference-properties)
  - [Azure provisioning & deployer](#azure-provisioning--deployer)
  - [Azure deployer interactive command handling](#azure-deployer-interactive-command-handling)
  - [Azure resource idempotency & existing resources](#azure-resource-idempotency--existing-resources)
  - [Compute image deployment](#compute-image-deployment)
  - [Module-scoped Bicep deployment](#module-scoped-bicep-deployment)
- [Docker & container tooling](#docker--container-tooling)
  - [Docker Compose Aspire Dashboard forwarding headers](#docker-compose-aspire-dashboard-forwarding-headers)
  - [Container build customization](#container-build-customization)
- [Publishing & interactions](#publishing--interactions)
  - [Publishing progress & activity reporting](#publishing-progress--activity-reporting)
  - [Parameter & interaction API updates](#parameter--interaction-api-updates)
- [Localization & UX consistency](#localization--ux-consistency)
- [Reliability & diagnostics](#reliability--diagnostics)
- [Minor enhancements](#minor-enhancements)

📢 .NET Aspire 9.5 is the next minor version release of .NET Aspire. It supports:

- .NET 8.0 Long Term Support (LTS)
- .NET 9.0 Standard Term Support (STS)
- .NET 10.0 Preview 6

If you have feedback, questions, or want to contribute to .NET Aspire, collaborate with us on [:::image type="icon" source="../media/github-mark.svg" border="false"::: GitHub](https://github.com/dotnet/aspire) or join us on our new [:::image type="icon" source="../media/discord-icon.svg" border="false"::: Discord](https://aka.ms/aspire-discord) to chat with the team and other community members.

It's important to note that .NET Aspire releases out-of-band from .NET releases. While major versions of Aspire align with major .NET versions, minor versions are released more frequently. For more information on .NET and .NET Aspire version support, see:

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy): Definitions for LTS and STS.
- [.NET Aspire support policy](https://dotnet.microsoft.com/platform/support/policy/aspire): Important unique product lifecycle details.

## Upgrade to .NET Aspire 9.5

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

## CLI improvements

### `aspire exec` command enhancements

The `aspire exec` command allows you to execute commands within the context of your Aspire application environment, inheriting environment variables and configuration from your app model resources.

#### New in 9.5

Building on the 9.4 preview, version 9.5 adds several key improvements:

- `--workdir` (`-w`) flag to run commands inside a specific working directory (#10912)
- Fail-fast argument validation with clearer error messages (#10606)
- Improved help and usage text for better developer experience (#10598)

#### Basic usage examples

```bash
# Execute database migrations with environment variables from your app model
aspire exec --resource my-api -- dotnet ef database update

# Run commands in a specific working directory
aspire exec --resource worker --workdir /app/tools -- dotnet run

# Wait for resource to start before executing command
aspire exec --start-resource my-worker -- npm run build
```

#### Command syntax

- Use `--resource` to execute immediately when AppHost starts
- Use `--start-resource` to wait for the resource to be running first
- Use `--workdir` to specify the working directory for the command
- Use `--` to separate aspire options from the command to execute

> [!IMPORTANT]
> 🧪 **Feature Flag**: The `aspire exec` command requires explicit enablement with:
>
> ```bash
> aspire config set features.execCommandEnabled true
> ```

### Robust orphan detection

Resilient to PID reuse via `ASPIRE_CLI_STARTED` timestamp + PID verification:

```text
Detected orphaned prior AppHost process (PID reused). Cleaning up...
```

### Package channel & templating enhancements

New packaging channel infrastructure lets `aspire add` and templating flows surface stable vs pre-release channels with localized UI.

### Improved cancellation & CTRL-C UX

- CTRL-C guidance message
- Fix for stall on interrupt

### New `aspire update` command (preview)

The new `aspire update` command helps you keep your Aspire projects current by automatically detecting and updating outdated packages and templates.

#### Basic usage

```bash
# Analyze and update out-of-date Aspire packages & templates
aspire update
```

#### Update command features

- **Automated package detection**: Finds outdated Aspire NuGet packages while respecting channel configurations
- **Diamond dependency resolution**: Intelligently handles complex dependency graphs without duplicate updates (#11145)
- **Enhanced reporting**: Colorized output with detailed summary of changes (#11148)
- **Channel awareness**: Respects your configured Aspire channel (preview, stable, etc.)
- **Safe updates**: Validates package compatibility before applying changes

#### Sample output

```text
🔍 Scanning for outdated Aspire packages...

Found 3 packages to update:
  ✨ Aspire.Hosting → 9.5.0 (was 9.4.1)
  ✨ Aspire.Hosting.Redis → 9.5.0 (was 9.4.1)  
  ✨ Aspire.Microsoft.Extensions.Configuration → 9.5.0 (was 9.4.1)

📦 Updating packages...
  ✅ Updated 3 packages successfully
  ⚠️  Review breaking changes in release notes

🎉 Update completed! Your project is now using Aspire 9.5.0
```

> [!IMPORTANT]
> 🧪 **Preview Feature**: The `aspire update` command is in preview and may change before general availability.

### Enhanced markdown and styling support

Extended markdown rendering support (#10815) with improved developer experience:

#### New capabilities

- **Code fences** with syntax highlighting for better readability
- **Rich text formatting** including emphasis, bold, and inline code
- **Structured lists** with bullet points and numbering
- **Safe markup escaping** to prevent XSS and rendering issues (#10462)
- Purple styling for default values in prompts (#10474)

### Channel-aware `aspire add` & templating

New packaging channel infrastructure (#10801, #10899) adds stable vs pre-release channel selection inside interactive flows:

- Channel menu for `aspire add` when multiple package qualities exist
- Pre-release surfacing with localized labels
- Unified PackagingService powering add + template selection

### Smarter package prefetching

Refactored NuGet prefetch architecture (#11120) reducing UI lag during `aspire new` on macOS (#11069) and enabling command-aware caching. Temporary NuGet config improvements ensure wildcard mappings (#10894).

### Orphan & runtime diagnostics

- Robust orphan AppHost detection via start timestamp (#10673)
- .NET SDK availability check with actionable errors (#10525)
- Container runtime health surfaced earlier (shared infra)

### Localization & resource strings

Moved hardcoded CLI strings to `.resx` (#10504) and enabled multi-language builds (numerous OneLocBuild commits). Command help, prompts, and errors are now localizable.

### Developer ergonomics

- AppHost debug/run selection surfaced in extension & CLI integration (#10877, #10369)
- Relative path included in AppHost status messages + TUI dashboard (#11132)
- Clean Spectre Console debug logging with reduced noise (#11125)
- Improved friendly name generation & pre-release submenu in `aspire add` (#10485)
- Directory safety check for `aspire new` (#10496) and consistent template inputs (#10444, #10508)

### Command infrastructure & interactions

- Context-sensitive completion messages for publish/deploy (#10501)
- Markdown-to-Spectre converter foundation reuse (#10815)
- Interaction answer typing change (`object`) for future extensibility (#10480)

### Stability & error handling

- Dashboard startup hang eliminated via `ResourceFailedException` (#10567)
- CTRL-C stall fix (#10962) + guidance message (#10203)
- Improved package channel error surfaces & prefetch retry logic (#11120)

> The `aspire exec` and `aspire update` commands remain in preview behind feature flags; behavior may change in a subsequent release.

## Dashboard enhancements

### Deep-linked telemetry navigation

The dashboard now provides seamless navigation between different telemetry views with interactive elements in property grids. Trace IDs, span IDs, resource names, and log levels become clickable buttons for one-click navigation (#10648).

#### How it works

- **Trace IDs**: Click to view the complete distributed trace
- **Span IDs**: Navigate directly to specific trace spans
- **Resource names**: Jump to resource-specific telemetry views  
- **Log levels**: Filter logs by severity level instantly

This eliminates the need to manually copy/paste identifiers between different dashboard views, making debugging and monitoring much more efficient.

### Multi-resource console logs

A new "All" option in the console logs view streams logs from every running resource simultaneously (#10981).

#### Features

- **Unified log stream**: See logs from all resources in chronological order
- **Color-coded prefixes**: Each resource gets a deterministic color for easy identification  
- **Configurable timestamps**: Separate timestamp preference to reduce noise
- **Real-time updates**: Live streaming of log events across your application

#### Example log output

```text
[api      INF] Application starting up
[postgres INF] Database system is ready to accept connections  
[redis    INF] Server initialized, ready to accept connections
[api      INF] Connected to database successfully
```

### Custom resource icons

Resources can specify custom icons using `WithIconName()` for better visual identification in dashboard views (#10760).

#### Simple icon setup

```csharp
var postgres = builder.AddPostgres("database")
    .WithIconName("database");

var redis = builder.AddRedis("cache")
    .WithIconName("memory");

var api = builder.AddProject<Projects.Api>("api")
    .WithIconName("web-app");
```

#### Icon variant options

```csharp
// Available variants: Regular (outline) or Filled (solid, default)
var database = builder.AddPostgres("db")
    .WithIconName("database", ApplicationModel.IconVariant.Regular);

var api = builder.AddProject<Projects.Api>("api")
    .WithIconName("web-app", ApplicationModel.IconVariant.Filled);
```

> [!NOTE]
> The default icon variant is `Filled` if not specified.

This helps teams quickly identify different types of resources in complex applications with many services.

### Reverse proxy support

The dashboard now properly handles reverse proxy scenarios with explicit forwarded header mapping when enabled. This fixes common issues with authentication redirects and URL generation behind proxies like YARP (#10388).

#### Configuration

```bash
# Enable forwarded headers processing
export ASPIRE_DASHBOARD_FORWARDEDHEADERS_ENABLED=true
```

#### Supported scenarios

- **OpenID Connect authentication** works correctly behind reverse proxies
- **URL generation** respects the original client request scheme and host
- **Limited header processing** for security - only Host and X-Forwarded-Proto are processed
- **YARP integration** and other reverse proxy solutions

This is particularly useful for deployment scenarios where the dashboard is accessed through a load balancer or reverse proxy.

### Improved mobile experience

The mobile and desktop experience has been redesigned with better responsive layouts and improved usability across all dashboard pages (#10407).

#### Improvements

- **Responsive toolbars**: Automatically adapt to screen size
- **Touch-friendly controls**: Larger targets for mobile interaction  
- **Optimized layouts**: Better use of screen real estate on smaller devices
- **Consistent navigation**: Unified experience across desktop and mobile

### Enhanced resource management

Several improvements to resource management and debugging capabilities:

#### Resource organization

- **Sub-menu organization**: Resource action menus now use sub-menus to prevent overflow on complex applications (#10869)
- **Launch profile details**: Project resources now show their associated launch profile for easier debugging (#10906)
- **Improved navigation**: Better resource selection and navigation handling (#10848)

#### Debugging enhancements  

- **Direct launch profile access**: Quick access to the launch configuration used for each project
- **Resource state visibility**: Clearer indication of resource status and health
- **Action grouping**: Related resource actions are logically grouped for better discoverability

### Container runtime notifications

Smart notifications appear when Docker/Podman is installed but unhealthy, with automatic dismissal when runtime recovers (#11008). This provides immediate feedback when your container runtime needs attention, helping diagnose startup issues faster.

### UI improvements

- Error spans use consistent error styling (#10742)
- Better default icons for parameters and services (#10762)
- Improved navigation reliability (#10848)
- Enhanced port parsing (#10884)
- Message truncation for long log entries (#10882)
- Optional log line wrapping (#10271)
- Improved text visualizer dialog (#10964)

### Trace performance & integration

- Optimized trace detail page rendering (#10308)
- Embedded log entries within trace spans (#10281)
- Better span timing calculations (#10310)

### Localization & deployment

- Comprehensive dashboard localization with consolidated resource files
- Launch profile support with localized display names (#10906)
- Forwarded headers support for proxy/container scenarios (#10388)

## Integration changes and additions

### OpenAI hosting integration

The new `AddOpenAI` integration provides first-class support for modeling OpenAI endpoints and their associated models within your Aspire application graph.

#### OpenAI integration features

- **Single OpenAI endpoint** resource with child model resources using `AddModel`
- **Parameter-based API key** provisioning with `ParameterResource` support
- **Endpoint override** for local gateways, proxies, or self-hosted solutions
- **Resource referencing** so other projects automatically receive connection information

#### Basic usage example

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiKey = builder.AddParameter("openai-api-key", secret: true);

var openai = builder.AddOpenAI("openai")
    .WithApiKey(apiKey)
    .WithEndpoint("https://api.openai.com");

var chatModel = openai.AddModel("chat", "gpt-4o-mini");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(chatModel);

builder.Build().Run();
```

#### Local development scenario

```csharp
// Use with local OpenAI-compatible services
var localOpenAI = builder.AddOpenAI("local-openai")
    .WithApiKey(builder.AddParameter("local-api-key"))
    .WithEndpoint("http://localhost:11434"); // Ollama or similar

var localModel = localOpenAI.AddModel("local-chat", "llama3.2");
```

### GitHub Models typed catalog

Version 9.5 introduces a strongly-typed catalog for GitHub-hosted models, providing IntelliSense support and refactoring safety when working with AI models (#10986).

#### Benefits over string-based approach

- **Type safety**: Compile-time validation of model names
- **IntelliSense support**: Discover available models and providers  
- **Refactoring safety**: Rename and find references work correctly
- **Up-to-date catalog**: Daily automation ensures new models are available (#11040)

#### Usage examples

```csharp
// Before: String-based approach (error-prone)
var model = github.AddModel("chat", "gpt-4o-mini"); // Typos not caught

// After: Typed catalog approach  
var chatModel = github.AddModel("chat", GitHubModel.OpenAI.Gpt4oMini);
var claudeModel = github.AddModel("claude", GitHubModel.Anthropic.Claude3_5Sonnet);
var llamaModel = github.AddModel("llama", GitHubModel.Meta.Llama3_1_405B_Instruct);

// IntelliSense shows all available models grouped by provider
var embeddingModel = github.AddModel("embeddings", GitHubModel.OpenAI.TextEmbedding3Large);
```

#### Complete GitHub Models integration

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Configure GitHub Models with token
var githubToken = builder.AddParameter("github-token", secret: true);

var github = builder.AddGitHubModels("github-models")
    .WithToken(githubToken);

// Add multiple model types with strong typing
var chatModel = github.AddModel("gpt4o", GitHubModel.OpenAI.Gpt4o);
var fastModel = github.AddModel("gpt4o-mini", GitHubModel.OpenAI.Gpt4oMini);
var claudeModel = github.AddModel("claude", GitHubModel.Anthropic.Claude3_5Sonnet);

// Use in your applications
var aiService = builder.AddProject<Projects.AIService>("ai-service")
    .WithReference(chatModel)
    .WithReference(fastModel)
    .WithReference(claudeModel);

builder.Build().Run();
```

The typed catalog automatically updates daily, so newly published models on GitHub become available without waiting for an Aspire release.

```csharp
// Before (string literal prone to typos)
builder.AddGitHubModel("phi4", model: "microsoft/phi-4" );

// After (typed constant)
builder.AddGitHubModel("phi4", GitHubModel.Microsoft.Phi4);
```

This reduces magic strings, enables code navigation to model definitions, and aligns GitHub model usage with the existing Azure AI Foundry `AIFoundryModel` experience introduced in 9.5.

## App model enhancements

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
var worker = builder.AddProject<Projects.Worker>("worker")
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

**Breaking change**: Endpoint resolution in `WithEnvironment` callbacks now correctly resolves container hostnames instead of always using "localhost" (#8574).

#### Endpoint resolution behavior change

```csharp
var redis = builder.AddRedis("redis");

// Another container getting endpoint info from Redis container
var rabbitmq = builder.AddContainer("myapp", "mycontainerapp")
    .WithEnvironment(context =>
    {
        var endpoint = redis.GetEndpoint("tcp");
        var redisHost = endpoint.Property(EndpointProperty.Host);
        var redisPort = endpoint.Property(EndpointProperty.Port);

        // Previously: redisHost would always resolve to "localhost" 
        // Now: redisHost correctly resolves to "redis" (container name)
        context.EnvironmentVariables["REDIS_HOST"] = redisHost;
        context.EnvironmentVariables["REDIS_PORT"] = redisPort;
    })
    .WithReference(redis);
```

#### What you need to review

- **Container deployments**: Your apps will now receive correct container hostnames
- **Local development**: Localhost behavior preserved for non-containerized scenarios  
- **Connection strings**: Automatic connection strings continue to work as expected
- **Manual environment**: Review custom `WithEnvironment` calls that assume localhost

## API changes and enhancements

### OTLP telemetry protocol selection

Enhanced OpenTelemetry Protocol (OTLP) support with protocol selection capabilities, allowing you to choose between gRPC and HTTP protobuf transports for telemetry data.

#### Protocol options

```csharp
// Available protocol types
public enum OtlpProtocol
{
    Grpc = 0,           // Default: High performance, binary protocol
    HttpProtobuf = 1    // Alternative: HTTP-based transport
}
```

#### Protocol examples

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Configure OTLP telemetry with specific protocol
var api = builder.AddProject<Projects.Api>("api")
    .WithOtlpExporter(OtlpProtocol.HttpProtobuf);

// Use default gRPC protocol (recommended for performance)
var worker = builder.AddProject<Projects.Worker>("worker")
    .WithOtlpExporter();

// Configure multiple services with different protocols
var frontend = builder.AddProject<Projects.Frontend>("frontend")
    .WithOtlpExporter(OtlpProtocol.Grpc);

builder.Build().Run();
```

#### When to use each protocol

- **gRPC (default)**: Best performance, smaller payload size, ideal for production
- **HTTP Protobuf**: Better firewall compatibility, easier debugging, good for development

### Enhanced resource waiting patterns

New `WaitForStart` method provides more granular control over resource startup dependencies, complementing the existing `WaitFor` behavior (#10948).

#### Understanding wait behaviors

- **`WaitFor`**: Waits for dependency to be Running AND pass all health checks
- **`WaitForStart`**: Waits only for dependency to reach Running state (ignores health checks)

#### Basic example

```csharp
var postgres = builder.AddPostgres("postgres");
var redis = builder.AddRedis("redis");

var api = builder.AddProject<Projects.Api>("api")
    .WaitForStart(postgres)  // Wait for startup only
    .WaitFor(redis)          // Wait for healthy state
    .WithReference(postgres)
    .WithReference(redis);
```

#### Migration scenario

```csharp
// Database initialization pattern
var database = builder.AddPostgres("postgres");

var migrator = builder.AddProject<Projects.Migrator>("migrator")
    .WaitForStart(database)  // Start as soon as DB container is running
    .WithReference(database);

var api = builder.AddProject<Projects.Api>("api")
    .WaitFor(database)       // Wait for healthy database
    .WaitFor(migrator)       // Wait for migration completion
    .WithReference(database);
```

### ExternalService WaitFor behavior improvements

**Breaking change**: `WaitFor` operations now properly honor `ExternalService` health checks, ensuring dependent resources wait for external services to be healthy before starting (#10827).

#### Before vs After

```csharp
var externalApi = builder.AddExternalService("backend-api", "http://api.company.com")
    .WithHttpHealthCheck("/health/ready");

// Previously: Frontend would start even if external API health check failed
// Now: Frontend waits for external API to be healthy
var frontend = builder.AddProject<Projects.Frontend>("frontend")
    .WaitFor(externalApi)
    .WithReference(externalApi);
```

#### Migration guidance

If you need the old behavior (start without waiting for health):

```csharp
// Option 1: Remove WaitFor if dependency isn't critical
var frontend = builder.AddProject<Projects.Frontend>("frontend")
    .WithReference(externalApi); // Reference but don't wait

// Option 2: Use WaitForStart for startup-only dependency
var frontend = builder.AddProject<Projects.Frontend>("frontend")
    .WaitForStart(externalApi) // Wait for external service to start trying
    .WithReference(externalApi);
```

### Enhanced resource lifetime support

**Breaking change**: Resources like `ParameterResource`, `ConnectionStringResource`, and model resources now support lifecycle operations and can be used with `WaitFor` (#10851, #10842).

#### Enhanced lifecycle capabilities

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Parameters and connection strings now support WaitFor
var connectionString = builder.AddConnectionString("database");
var apiKey = builder.AddParameter("api-key", secret: true);

var api = builder.AddProject<Projects.Api>("api")
    .WaitFor(connectionString)  // Wait for connection string to be resolved
    .WaitFor(apiKey)           // Wait for parameter to be available
    .WithEnvironment("DB_CONNECTION", connectionString)
    .WithEnvironment("API_KEY", apiKey);

// GitHub Models also support lifecycle operations
var github = builder.AddGitHubModels("github");
var model = github.AddModel("gpt4", GitHubModel.OpenAI.Gpt4o);

var aiService = builder.AddProject<Projects.AIService>("ai-service")
    .WaitFor(model)  // Wait for model resource to be ready
    .WithReference(model);

builder.Build().Run();
```

### InteractionInput API improvements

**Breaking change**: The `InteractionInput` API has been updated to require a `Name` property while making `Label` optional for better form handling (#10835).

#### Migration example

```csharp
// Before (9.4 and earlier)
var input = new InteractionInput
{
    Label = "Database Password",
    InputType = InputType.SecretText,
    Required = true
};

// After (9.5+) 
var input = new InteractionInput
{
    Name = "database_password",    // Required: Form field name
    Label = "Database Password",   // Optional: Display label (defaults to Name)
    InputType = InputType.SecretText,
    Required = true
};
```

This change improves form processing and enables better integration with web-based interactions.

### Resource icon customization

Resources can now specify custom icons for better visual identification in the dashboard using the `WithIconName` method (#10760).

#### Basic icon usage

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Simple icon assignment
var postgres = builder.AddPostgres("database")
    .WithIconName("database");

var api = builder.AddProject<Projects.Api>("api")
    .WithIconName("cloud");

var redis = builder.AddRedis("cache")
    .WithIconName("memory");

builder.Build().Run();
```

#### Icon variants

```csharp
// Available variants: Regular (outline) or Filled (solid)
var database = builder.AddPostgres("db")
    .WithIconName("database", ApplicationModel.IconVariant.Regular);

var api = builder.AddProject<Projects.Api>("api")
    .WithIconName("web-app", ApplicationModel.IconVariant.Filled);
```

> [!NOTE]
> The default icon variant is `Filled` if not specified.

### MySQL password handling improvements

Enhanced password management for MySQL resources with consistent patterns across database integrations:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Secure password parameter
var password = builder.AddParameter("mysql-password", secret: true);

var mysql = builder.AddMySql("mysql")
    .WithPassword(password);

// Password can be updated during configuration
mysql.Resource.PasswordParameter = builder.AddParameter("new-mysql-password", secret: true);

// Environment-specific passwords
var devPassword = builder.Configuration["ConnectionStrings:MySQL:Password"];
if (!string.IsNullOrEmpty(devPassword))
{
    mysql.WithPassword(devPassword);
}

builder.Build().Run();
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

### MySQL password improvements

Consistent password handling across database resources:

```csharp
var mysql = builder.AddMySql("mysql")
  .WithPassword(builder.AddParameter("mysql-password", secret: true));

// Password can be modified during configuration
mysql.Resource.PasswordParameter = builder.AddParameter("new-password", secret: true);
```

### Remote & debugging experience

### SSH remote auto port forwarding

VS Code SSH sessions now get automatic port forwarding configuration just like Dev Containers and Codespaces:

```text
Remote SSH environment detected – configuring forwarded ports (dashboard, api, postgres).
```

### AppHost debugging in VS Code

The extension offers Run vs Debug for the AppHost. If the C# extension is present it launches under the debugger; otherwise a terminal with `dotnet watch` is used.

### Extension modernization

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

### Azure App Configuration emulator APIs

Run emulators locally with full configuration support:

```csharp
var appConfig = builder.AddAzureAppConfiguration("config")
  .RunAsEmulator(emulator => emulator
    .WithDataVolume("config-data")
    .WithHostPort(8080));
```

### Azure Storage emulator improvements

Updated Azurite to version 3.35.0, resolving health check issues that previously returned HTTP 400 responses (#10972). This improves the reliability of Azure Storage emulator health checks during development.

### Broader Azure resource capability surfacing

Several Azure hosting resource types now implement `IResourceWithEndpoints` enabling uniform endpoint discovery and waiting semantics:

- `AzureAIFoundryResource`
- `AzureAppConfigurationResource`
- `AzureKeyVaultResource`
- `AzurePostgresFlexibleServerResource`
- `AzureRedisCacheResource`

### Azure resource reference properties

New reference properties have been added to Azure PostgreSQL and Redis resources for custom connection string composition and individual component access (#11051, #11070).

#### AzurePostgresFlexibleServerResource enhancements

Three new reference properties enable custom connection string composition:

- **`HostName`** (`ReferenceExpression`): Returns PostgreSQL server hostname with port
- **`UserName`** (`ReferenceExpression?`): Returns username for password authentication (null for Entra ID)  
- **`Password`** (`ReferenceExpression?`): Returns password for password authentication (null for Entra ID)

```csharp
var postgres = builder.AddAzurePostgresFlexibleServer("database")
    .WithPasswordAuthentication()
    .RunAsContainer();

var db = postgres.AddDatabase("appdb");

// Custom JDBC connection string
var jdbc = ReferenceExpression.Create($"jdbc:postgresql://{postgres.Resource.HostName}/{db.Resource.DatabaseName}");

// Custom PostgreSQL connection string  
var connectionString = ReferenceExpression.Create(
    $"Host={postgres.Resource.HostName};Username={postgres.Resource.UserName};Password={postgres.Resource.Password};Database={db.Resource.DatabaseName}");
```

#### AzureRedisCacheResource enhancements

Two new reference properties enable custom Redis connection scenarios:

- **`HostName`** (`ReferenceExpression`): Returns Redis server hostname with port
- **`Password`** (`ReferenceExpression?`): Returns password when running as container (null in Azure mode)

```csharp
var redis = builder.AddAzureRedis("cache")
    .RunAsContainer();

// Custom Redis connection string
var customConnectionString = ReferenceExpression.Create($"{redis.Resource.HostName},password={redis.Resource.Password}");

// Access individual components
var hostName = redis.Resource.HostName; // e.g., "localhost:6379"
var password = redis.Resource.Password; // Available in container mode
```

### Azure provisioning & deployer

9.5 delivers the first iteration of the Azure provisioning & deployment pipeline that unifies interactive prompting, Bicep compilation, and mode-specific behavior (run vs publish) across Azure resources:

- New provisioning contexts separate run-mode and publish-mode flows ([#11094](https://github.com/dotnet/aspire/pull/11094)).
- Graph-based dependency planning (`ResourceDeploymentGraph`) ensures correct ordering of resource provisioning.
- Improved error handling and idempotency for `AddAsExistingResource` across all Azure resources ([#10562](https://github.com/dotnet/aspire/issues/10562)).
- Support for deploying compute images and resources (custom images referenced in your environment) ([#11030](https://github.com/dotnet/aspire/pull/11030)).
- Deploy individual Bicep modules instead of a monolithic `main.bicep` for clearer failure isolation and faster iteration ([#11098](https://github.com/dotnet/aspire/pull/11098)).
- Localized interaction + notification strings across all provisioning prompts (multiple OneLocBuild PRs).

Provisioning automatically prompts for required values only once per run, caches results, and reuses them in publish-mode without re-prompting. This reduces friction when iterating locally while maintaining reproducibility for production publish.

### Azure deployer interactive command handling

The AppHost now wires Azure provisioning prompts into the standard interaction system (initial work in [#10038](https://github.com/dotnet/aspire/pull/10038), extended in [#10792](https://github.com/dotnet/aspire/pull/10792) and [#10845](https://github.com/dotnet/aspire/pull/10845)). This enables:

- Consistent UX for parameter entry (names, descriptions, validation)
- Localized prompt text
- Support for non-interactive scenarios via pre-supplied parameters

### Azure resource idempotency & existing resources

Calling `AddAsExistingResource` is now idempotent across Azure hosting resource builders; repeated calls no longer cause duplicate annotations or inconsistent behavior ([#10562](https://github.com/dotnet/aspire/issues/10562)). This improves reliability when composing reusable extension methods.

### Compute image deployment

You can now reference and deploy custom compute images as part of Azure environment provisioning ([#11030](https://github.com/dotnet/aspire/pull/11030)). This lays groundwork for richer VM/container hybrid topologies.

### Module-scoped Bicep deployment

Instead of generating a single aggregated template, 9.5 deploys individual Bicep modules ([#11098](https://github.com/dotnet/aspire/pull/11098)). Failures surface with more precise context and partial successes require less rework.

## Docker & container tooling

### Docker Compose Aspire Dashboard forwarding headers

`AddDockerComposeEnvironment(...).WithDashboard()` gained `WithForwardedHeaders()` to enable forwarded `Host` and `Proto` handling for dashboard scenarios behind reverse proxies or compose networks ([#11080](https://github.com/dotnet/aspire/pull/11080)). This mirrors the standalone dashboard forwarded header support and fixes auth redirect edge cases.

```csharp
builder.AddDockerComposeEnvironment("env")
  .WithComposeFile("docker-compose.yml")
  .WithDashboard(d => d.WithForwardedHeaders());
```

### Container build customization

`ContainerBuildOptions` support (commit [#10074](https://github.com/dotnet/aspire/pull/10074)) enables customizing the underlying `dotnet publish` invocation when Aspire builds project-sourced container images (for example to change configuration, trimming, or pass additional MSBuild properties). Use the new options hook on the project container image configuration to set MSBuild properties instead of maintaining a custom Dockerfile. (Exact API surface is intentionally summarized here to avoid drift; see API docs for `ContainerBuildOptions` in the hosting namespace for usage.)

## Publishing & interactions

### Publishing progress & activity reporting

`IPublishingActivityProgressReporter` was renamed to `IPublishingActivityReporter` and output formatting was reworked to provide clearer, structured progress (multiple commits culminating in improved messages). Expect more concise status lines and actionable error sections when using `aspire publish`.

### Parameter & interaction API updates

- `ParameterResource.Value` is now obsolete: switch to `await parameter.GetValueAsync()` or inject parameter resources directly ([#10363](https://github.com/dotnet/aspire/pull/10363)). This change improves async value acquisition and avoids accidental blocking.
- Interaction inputs enforce server-side validation and required `Name` property (breaking, [#10835](https://github.com/dotnet/aspire/pull/10835)).
- New notification terminology (renamed from MessageBar, [#10449](https://github.com/dotnet/aspire/pull/10449)).
- `ExecuteCommandResult` now includes a `Canceled` property to track whether command execution was canceled by the user or system.

Migration example:

```csharp
// Before (deprecated)
var value = myParam.Value;

// After
var value = await myParam.GetValueAsync();
```

## Localization & UX consistency

Extensive localization landed across the AppHost, Azure provisioning, interactions, launch profiles, and dashboard-facing messages (multiple OneLocBuild commits). Resource strings replace hardcoded literals, enabling translated tooling experiences out-of-the-box.

## Reliability & diagnostics

- New `ResourceStoppedEvent` provides lifecycle insight when resources shut down or fail ([#11103](https://github.com/dotnet/aspire/pull/11103)):

```csharp
builder.AddProject<Projects.Api>("api")
  .OnResourceStopped(async (resource, evt, ct) =>
  {
      // Handle resource stopped event - log cleanup, notify other services, etc.
      Console.WriteLine($"Resource {resource.Name} stopped with event: {evt.ResourceEvent}");
      await NotifyDependentServices(resource.Name, ct);
  });
```

- Hardened container runtime health checks now block image build when the runtime is unhealthy rather than failing later ([#10399](https://github.com/dotnet/aspire/pull/10399), [#10402](https://github.com/dotnet/aspire/pull/10402)).
- Dashboard startup failure now surfaces an immediate, clear exception instead of hanging (`ResourceFailedException`, [#10567](https://github.com/dotnet/aspire/pull/10567)).
- Version checking messages localized and de-duplicated ([#11017](https://github.com/dotnet/aspire/pull/11017)).

## Minor enhancements

- Server-side validation of interaction inputs ([#10527](https://github.com/dotnet/aspire/pull/10527)).
- Custom resource icons section already noted now also apply to project & container resources via unified annotation.
- Launch profile localization and model surfaced in dashboard resource details ([#10906](https://github.com/dotnet/aspire/pull/10906)).
