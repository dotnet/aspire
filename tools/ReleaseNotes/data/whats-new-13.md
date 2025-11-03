---
title: What's new in Aspire 13.0
description: Learn what's new in Aspire 13.0.
ms.date: 11/03/2025
---

# What's new in Aspire 13.0

📢 Aspire 13.0 is a major version release of Aspire, introducing transformational features for cloud-native application development. It requires:

- .NET 10 SDK or later

If you have feedback, questions, or want to contribute to Aspire, collaborate with us on [:::image type="icon" source="../media/github-mark.svg" border="false"::: GitHub](https://github.com/dotnet/aspire) or join us on [:::image type="icon" source="../media/discord-icon.svg" border="false"::: Discord](https://aka.ms/aspire-discord) to chat with the team and other community members.

## Table of contents

- [Upgrade to Aspire 13.0](#upgrade-to-aspire-130)
- [Major new features](#major-new-features)
  - [Distributed Application Pipeline](#distributed-application-pipeline)
  - [Dashboard AI Assistant](#dashboard-ai-assistant)
  - [Dockerfile Builder API](#dockerfile-builder-api-experimental)
  - [Certificate Management](#certificate-management)
- [CLI and tooling](#-cli-and-tooling)
  - [aspire init command](#aspire-init-command)
  - [aspire update command](#aspire-update-command)
  - [aspire do command](#aspire-do-command-pipeline-entry-point)
  - [aspire cache command](#aspire-cache-command)
  - [Single-file AppHost support](#single-file-apphost-support)
  - [Automatic .NET SDK installation](#automatic-net-sdk-installation)
  - [Other CLI improvements](#other-cli-improvements)
- [Dashboard enhancements](#-dashboard-enhancements)
  - [Model Context Protocol (MCP) server](#model-context-protocol-mcp-server)
  - [Console logs refactoring](#console-logs-refactoring)
  - [Trace and telemetry improvements](#trace-and-telemetry-improvements)
  - [Markdown rendering](#markdown-rendering)
  - [UI and accessibility improvements](#ui-and-accessibility-improvements)
- [App model enhancements](#-app-model-enhancements)
  - [C# app support](#c-app-support)
  - [Network identifiers](#network-identifiers)
  - [Dynamic input system](#dynamic-input-system)
  - [Reference and connection improvements](#reference-and-connection-improvements)
  - [Deployment state management](#deployment-state-management)
  - [Container files management](#container-files-management)
  - [Event system](#event-system)
- [Breaking changes](#-breaking-changes)

## Upgrade to Aspire 13.0

> [!IMPORTANT]
> Aspire 13.0 is a major version release with breaking changes. Please review the [Breaking changes](#-breaking-changes) section before upgrading.

The easiest way to upgrade to Aspire 13.0 is using the `aspire update` command:

1. Update the Aspire CLI to the latest version:

    ```bash
    # Bash
    curl -sSL https://aspire.dev/install.sh | bash

    # PowerShell
    iex "& { $(irm https://aspire.dev/install.ps1) }"
    ```

2. Update your Aspire project using the [`aspire update` command](#aspire-update-command):

    ```bash
    aspire update
    ```

    This command will:
    - Update the Aspire.AppHost.Sdk version in your AppHost project
    - Update all Aspire NuGet packages to version 13.0
    - Handle dependency resolution automatically
    - Support both regular projects and Central Package Management (CPM)

3. Update your Aspire templates:

    ```bash
    dotnet new install Aspire.ProjectTemplates
    ```

> [!NOTE]
> If you're upgrading from Aspire 8.x, follow [the upgrade guide](../get-started/upgrade-to-aspire-9.md) first to upgrade to 9.x, then upgrade to 13.0.

## Major new features

### Distributed Application Pipeline

Aspire 13.0 introduces a comprehensive pipeline system for coordinating build, deployment, and publishing operations. This new architecture provides a foundation for orchestrating complex deployment workflows with built-in support for step dependencies, parallel execution, and detailed progress reporting.

The pipeline system replaces the previous publishing infrastructure with a more flexible, extensible model that allows resource-specific deployment logic to be decentralized and composed into larger workflows.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Resources participate in the pipeline automatically
var api = builder.AddProject<Projects.Api>("api");
var database = builder.AddPostgres("postgres");

// Pipeline handles coordination, dependencies, and reporting
await builder.Build().RunAsync();
```

The pipeline system includes:

- **Step abstraction**: Define custom deployment steps with dependency tracking
- **Parallel execution**: Steps run concurrently when dependencies allow
- **Progress reporting**: Built-in activity reporting for deployment operations
- **Resource-specific logic**: Each resource type can define its own deployment behavior
- **Output management**: Centralized pipeline output service for artifacts

For more details on the pipeline architecture, see [Deployment pipeline documentation](../deployment/pipeline-architecture.md).

### Dashboard AI Assistant

The Dashboard now includes an integrated AI assistant that provides context-aware help, error explanations, and intelligent suggestions directly within the dashboard experience.

Key features include:

- **Interactive chat interface**: Converse with the AI assistant about your application
- **Context-aware assistance**: The assistant understands your resource configuration and telemetry
- **Error explanations**: Click "Explain Errors" on traces and logs to get AI-powered insights
- **Markdown support**: Rich formatting with code blocks and syntax highlighting
- **GenAI telemetry visualization**: Enhanced visualization of AI operations in your application

The AI assistant integration makes debugging and understanding distributed applications more intuitive by providing intelligent insights based on your application's actual runtime behavior.

### Dockerfile Builder API (Experimental)

Aspire 13.0 introduces an experimental programmatic Dockerfile generation API that allows you to define Dockerfiles using C# code with a composable, type-safe API.

> [!IMPORTANT]
> 🧪 **Experimental Feature**: The Dockerfile Builder API is experimental and may change before general availability.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Define a Dockerfile programmatically
var app = builder.AddContainer("myapp", "myapp")
    .WithDockerfileBuilder(dockerfile =>
    {
        var buildStage = dockerfile.AddStage("build", "mcr.microsoft.com/dotnet/sdk:10.0");
        buildStage.AddWorkdir("/src");
        buildStage.AddCopy(".", "/src");
        buildStage.AddRun("dotnet build -c Release");

        var publishStage = dockerfile.AddStage("publish", "build");
        publishStage.AddRun("dotnet publish -c Release -o /app");

        var finalStage = dockerfile.AddStage("final", "mcr.microsoft.com/dotnet/aspnet:10.0");
        finalStage.AddWorkdir("/app");
        finalStage.AddCopyFrom("publish", "/app", ".");
        finalStage.AddEntrypoint("dotnet", "myapp.dll");
    });

await builder.Build().RunAsync();
```

The Dockerfile Builder provides:

- **Multi-stage build support**: Define build, publish, and runtime stages
- **Type-safe API**: IntelliSense and compile-time validation
- **Composable statements**: Build complex Dockerfiles from reusable components
- **Dynamic generation**: Generate Dockerfiles based on runtime conditions
- **Factory pattern**: Share Dockerfile templates across multiple resources with `WithDockerfileFactory`

Alternatively, use the factory pattern for simple string-based Dockerfile generation:

```csharp
var app = builder.AddContainer("myapp", "myapp")
    .WithDockerfileFactory((context) =>
    {
        return """
            FROM mcr.microsoft.com/dotnet/aspnet:10.0
            WORKDIR /app
            COPY . .
            ENTRYPOINT ["dotnet", "myapp.dll"]
            """;
    });
```

This experimental feature enables sophisticated container image construction scenarios while maintaining the developer experience of working in C#.

### Certificate Management

Aspire 13.0 introduces comprehensive certificate management capabilities for handling custom certificate authorities and developer certificate trust in containerized environments.

#### Certificate Authority Collections

Define and manage custom certificate collections for your distributed applications:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a certificate authority collection
var certs = builder.AddCertificateAuthorityCollection("custom-certs")
    .WithCertificatesFromFile("./certs/my-ca.pem")
    .WithCertificatesFromStore(
        StoreName.CertificateAuthority,
        StoreLocation.LocalMachine,
        allowInvalid: false);

// Use the certificate collection in your resources
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(certs);

await builder.Build().RunAsync();
```

#### Developer Certificate Trust

Automatically configure container trust for developer certificates on Mac and Linux:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api")
    .WithDeveloperCertificateTrust(); // Automatically trust dev certs in container

await builder.Build().RunAsync();
```

Certificate management features include:

- **Multiple certificate sources**: Load from PEM files, Windows certificate stores, or programmatically
- **Flexible trust scoping**: System-level, append, override, or no trust
- **Container certificate paths**: Customize where certificates are placed in containers
- **Developer certificate support**: Automatic trust configuration for local development
- **Environment variable control**: Configure certificate behavior through environment variables

These features enable production-ready certificate handling in development, testing, and deployment scenarios.

## 🛠️ CLI and tooling

### aspire init command

The new `aspire init` command provides a streamlined, interactive experience for initializing Aspire solutions with comprehensive project setup and configuration.

```bash
# Initialize a new Aspire solution - interactive prompts guide you through setup
aspire init
```

When you run `aspire init`, the CLI will:

- **Discover existing solutions**: Automatically finds and updates solution files in the current directory
- **Create single-file AppHost**: If no solution exists, creates a single-file AppHost for quick starts
- **Add projects intelligently**: Prompts to add projects to your app host
- **Configure service defaults**: Sets up service defaults referencing automatically
- **Setup NuGet.config**: Creates package source mappings for Aspire packages
- **Manage template versions**: Interactively selects the appropriate template version

The init command simplifies the initial project setup through an interactive workflow that ensures consistent configuration across team members.

### aspire update command

The `aspire update` command helps keep your Aspire projects current by automatically detecting and updating outdated packages, templates, and even the CLI itself.

```bash
# Update all Aspire packages in the current project
aspire update

# Update a specific project
aspire update --project ./src/MyApp.AppHost

# Update with recursive directory search
aspire update --project ./src --recursive

# Update the Aspire CLI itself
aspire update --self
```

The update command includes:

- **Central package management (CPM) support**: Handles CPM-enabled solutions
- **Diamond dependency resolution**: Intelligently manages complex dependency graphs without duplicates
- **Single-file app host support**: Updates single-file app hosts
- **XML fallback parsing**: Resilient parsing for unresolvable AppHost SDKs
- **Enhanced visual presentation**: Colorized output with detailed summary of changes
- **Channel awareness**: Respects configured Aspire channels (stable, preview, staging)

The update command makes it easy to stay current with Aspire releases while maintaining compatibility across your project dependencies.

### aspire do command (Pipeline Entry Point)

The new `aspire do` command serves as a general-purpose pipeline entry point for executing deployment and build operations.

```bash
# Execute a specific pipeline step (e.g., step)
aspire do step

# Execute with custom output path
aspire do publish --output-path ./artifacts

# Execute with specific environment
aspire do deploy --environment Production

# Execute with verbose logging
aspire do deploy --log-level debug
```

This command provides access to the new [Distributed Application Pipeline](#distributed-application-pipeline) system, enabling fine-grained control over deployment workflows. The step name is specified as an argument, and the command automatically executes all dependency steps.

### aspire cache command

The `aspire cache` command provides management capabilities for the Aspire CLI's disk cache, which stores NuGet package metadata to improve performance.

```bash
# Clear the Aspire CLI cache
aspire cache clear
```

The cache system includes:

- **Expiry management**: Automatic cleanup of expired cache entries
- **Version-aware cleanup**: Removes outdated package metadata
- **Selective caching control**: Configure what gets cached
- **Testable interface**: IDiskCache interface for testing scenarios

The disk cache significantly improves the performance of `aspire add` and `aspire new` commands by reducing repeated NuGet package queries.

### Single-file AppHost support

Aspire 13.0 introduces comprehensive support for single-file app hosts, allowing you to define your entire distributed application in a single `.cs` file without a project file.

```csharp
// apphost.cs
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");
var database = builder.AddPostgres("postgres");

api.WithReference(database);

await builder.Build().RunAsync();
```

Single-file app host support includes:

- **Template support**: Use the `aspire-apphost-singlefile` template via `aspire new`
- **Full CLI integration**: Works seamlessly with `aspire run`, `aspire deploy`, `aspire publish`, `aspire add`, `aspire update`
- **Launch profile support**: Full debugging and launch configuration support
- **Python integration**: Enhanced Python and JavaScript application support

> [!NOTE]
> Single-file app hosts require .NET 10.0 SDK or later.

### Automatic .NET SDK installation (Preview)

The Aspire CLI includes a preview feature for automatically installing required .NET SDK versions when they're missing.

> [!IMPORTANT]
> This is a preview feature that is **not enabled by default**. To use automatic SDK installation, enable it with:
> ```bash
> aspire config set features.dotnetSdkInstallationEnabled true
> ```

Once enabled, the CLI will automatically install missing SDKs:

```bash
# With the feature enabled, the CLI will automatically install the required SDK
aspire run

# Installing .NET SDK 10.0.100...
# ✅ SDK installation complete
# Running app host...
```

The automatic SDK installation feature provides:

- **Embeds installation scripts**: dotnet-install.sh and dotnet-install.ps1 as resources
- **Cross-platform support**: Works on Windows, macOS, and Linux
- **Version detection**: Automatically detects required SDK versions
- **Fallback support**: Provides alternative installation options if automatic installation fails

When enabled, this preview feature can improve the onboarding experience for new team members and CI/CD environments.

### Other CLI improvements

#### NuGet package management
- Enhanced package source mapping with `NuGetConfigMerger`
- Package channel service with quality filtering (stable, preview, staging)
- User confirmation prompts for NuGet.config changes
- Staging channel support for dogfooding builds

#### Template enhancements
- New Python starter template (`aspire-py-starter`) for Python and JavaScript applications
- Improved template version selection display
- Better template discovery and filtering

#### Markdown support
- Comprehensive markdown rendering with syntax support
- MarkdownToSpectreConverter for CLI output
- Code block support with syntax highlighting
- Multi-line handling

#### Non-interactive mode
- `--non-interactive` flag for CI/CD environments
- `ASPIRE_PLAYGROUND` environment variable for forcing interactive mode
- Clean, structured output for automation scenarios

#### SSH Remote support
- Automatic port forwarding configuration for VS Code SSH Remote
- Consistent experience with Devcontainers and Codespaces
- Environment variable detection (`SSH_CONNECTION`, `VSCODE_IPC_HOOK_CLI`)

## 📊 Dashboard enhancements

### Model Context Protocol (MCP) server

The Dashboard now includes a Model Context Protocol (MCP) server implementation, enabling integration with external AI tools and development environments.

The MCP server provides:

- **Resource tools**: `AspireResourceMcpTools` for querying and managing resources
- **Telemetry tools**: `AspireTelemetryMcpTools` for accessing traces, logs, and metrics
- **VS Code integration**: Tab support in MCP dialog
- **Improved results**: Resource links and shortened names for better readability
- **Server configuration**: Flexible MCP server dialog settings

This enables AI assistants like Claude and other MCP-compatible tools to directly interact with your Aspire applications, querying resources, analyzing telemetry, and providing intelligent insights.

### Console logs refactoring

The console logs page has been refactored using an item provider pattern with enhanced multi-resource viewing capabilities.

**Features:**

- **"All" resources view**: See logs from all resources in chronological order with colored prefixes
- **Shared logging channel**: Efficient log collection across resources
- **Smart timestamps**: Configurable timestamp display to reduce noise
- **Wrap log lines**: Optional line wrapping for long log entries
- **Race condition fixes**: Improved reliability when viewing logs from multiple resources

```text
[api      ] Application starting up
[postgres ] Database system is ready to accept connections
[redis    ] Server initialized
[api      ] Connected to database successfully
```

The refactored logs page makes it easier to understand the interaction between services in your distributed application.

### Trace and telemetry improvements

#### Trace details enhancements
- **Collapse/expand all**: Quickly expand or collapse all spans in a trace
- **Resource column**: See which resource produced each span
- **Span actions menu**: GenAI link and other actions from span details
- **Destination display**: Shows span destination information
- **Performance improvements**: Faster rendering for large traces

#### Span filtering
- **Span type selector**: Filter spans by type (HTTP, Database, Messaging, etc.)
- **Cloud type filter**: Filter by cloud provider or service
- **Filter grouping**: Organized filter labels for better UX
- **Type classification**: Automatic span type detection

#### Structured logs
- **Enhanced display**: Improved structured log entry visualization
- **Log level filtering**: Quick filter by log level (Error, Warning, Info, etc.)
- **Filter deduplication**: Cleaner filter lists

### Markdown rendering

The Dashboard now includes comprehensive markdown rendering capabilities with code block support and syntax highlighting.

**Features:**

- **MarkdownRenderer component**: Rich markdown display throughout the dashboard
- **Code block highlighting**: `HighlightedCodeBlockRenderer` with syntax highlighting
- **Custom extensions**: `AspireEnrichmentParser` for Aspire-specific markdown
- **Multi-language support**: Syntax highlighting for C#, JavaScript, Python, and more
- **Responsive styling**: Markdown CSS optimized for dashboard layouts

This enables richer documentation, better error messages, and improved AI assistant responses with properly formatted code examples.

### UI and accessibility improvements

#### Visual enhancements
- **Updated FluentUI**: FluentUI 4.13.0 with improved components
- **Accent color refactoring**: Consistent color usage across the dashboard
- **Mobile/desktop toolbar**: Responsive toolbars that adapt to screen size
- **Vertical menu overflow**: Better handling of long menu lists
- **Span name truncation**: Ellipsis for long span names

#### Interaction improvements
- **ComboBox filtering**: Enhanced filtering in dropdown selections
- **Default values**: Better support for choice input defaults
- **Parameter descriptions**: Custom input rendering for parameters
- **Dynamic inputs**: Load inputs based on other input values
- **Server-side validation**: Validation of interaction inputs

#### Health check display
- **Timestamp display**: Shows when health checks last ran
- **"Just now" indicator**: Recent health check indication
- **Tooltip details**: Last run time in tooltips
- **Unhealthy state display**: Clear visualization of unhealthy resources

## 🖥️ App model enhancements

### C# app support

Aspire 13.0 adds first-class support for C# file-based applications, enabling you to add C# apps without full project files to your distributed application.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a C# file-based app
var app = builder.AddCSharpApp("myapp", "./path/to/app.cs")
    .WithReference(database);

await builder.Build().RunAsync();
```

This feature works seamlessly with .NET 10 SDK's file-based application support and includes:

- **CSharpAppResource**: New resource type for file-based apps
- **Launch profile support**: Debugging support for file-based apps
- **Service discovery**: File-based apps participate in service discovery

### Network identifiers

Aspire 13.0 introduces `NetworkIdentifier` for better network context awareness in endpoint resolution.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");

// Get endpoint with specific network context
var hostEndpoint = api.GetEndpoint("http", NetworkIdentifier.Host);
var containerEndpoint = api.GetEndpoint("http", KnownNetworkIdentifiers.DefaultAspireContainerNetwork);

await builder.Build().RunAsync();
```

Network identifier features:

- **Context-aware endpoint resolution**: Resolve endpoints based on the network context (host, container network, etc.)
- **Known network identifiers**: Predefined identifiers for common scenarios (`LocalhostNetwork`, `DefaultAspireContainerNetwork`, `PublicInternet`)
- **AllocatedEndpoint changes**: Endpoints now include their `NetworkID` instead of a container host address
- **Better container networking**: Improved support for container-to-container communication scenarios

### Dynamic input system (Experimental)

The new dynamic input system allows inputs to load options based on other input values, enabling sophisticated parameter prompting scenarios like cascading dropdowns.

> [!NOTE]
> This is an experimental feature marked with `[Experimental("ASPIREINTERACTION001")]`.

```csharp
var interactionService = serviceProvider.GetRequiredService<IInteractionService>();

var inputs = new List<InteractionInput>
{
    // First input - static options
    new InteractionInput
    {
        Name = "Region",
        InputType = InputType.Choice,
        Label = "Azure Region",
        Required = true,
        Options =
        [
            KeyValuePair.Create("eastus", "East US"),
            KeyValuePair.Create("westus", "West US"),
            KeyValuePair.Create("centralus", "Central US")
        ]
    },

    // Second input - dynamically loads based on first input
    new InteractionInput
    {
        Name = "Subscription",
        InputType = InputType.Choice,
        Label = "Subscription",
        Required = true,
        Disabled = true, // Initially disabled until region is selected
        DynamicLoading = new InputLoadOptions
        {
            LoadCallback = async (context) =>
            {
                // Access the region input value
                var region = context.AllInputs["Region"].Value;

                if (!string.IsNullOrEmpty(region))
                {
                    // Load subscriptions for the selected region
                    var subscriptions = await GetSubscriptionsForRegionAsync(region, context.CancellationToken);

                    context.Input.Options = subscriptions;
                    context.Input.Disabled = false; // Enable input when options are loaded
                }
            },
            DependsOnInputs = ["Region"] // Reload when Region changes
        }
    }
};

var result = await interactionService.PromptInputsAsync(
    "Azure Configuration",
    "Select your Azure region and subscription",
    inputs,
    cancellationToken);
```

Dynamic input features:

- **InputLoadOptions**: Define callback-based option loading with `LoadCallback`
- **LoadInputContext**: Access other inputs via `context.AllInputs[name]`, cancellation token, and the current input via `context.Input`
- **Dependency tracking**: Specify dependencies with `DependsOnInputs` array to trigger reloading
- **Dynamic enable/disable**: Control `context.Input.Disabled` based on loaded data
- **Async support**: Load options from APIs, databases, or external services

### Reference and connection improvements

#### Named references

Reference resources with explicit names for better service discovery control:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres");

// Add a named reference for service discovery
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(database, "primary-db");

await builder.Build().RunAsync();
```

#### Connection properties

Access individual connection string properties programmatically:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("appdb");

// Get individual connection properties
var host = postgres.GetConnectionProperty("Host");
var port = postgres.GetConnectionProperty("Port");
var username = postgres.GetConnectionProperty("Username");

// Combine properties for custom connection strings
var customConnectionString = postgres.CombineProperties(
    ("Host", host),
    ("Port", port),
    ("Username", username),
    ("Database", db.Resource.DatabaseName));

await builder.Build().RunAsync();
```

#### Endpoint reference enhancements

More flexible endpoint resolution with network context awareness:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");

// Get endpoint with network identifier context
var endpoint = api.GetEndpoint("http", NetworkIdentifier.Host);

// Remap host URL (address and port)
var remappedEndpoint = api.GetEndpoint("http")
    .WithHostUrlRemapping(newAddress: "custom-host", newPort: 8080);

await builder.Build().RunAsync();
```

#### Child relationships

Model parent-child relationships between resources:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var parent = builder.AddContainer("parent", "parent-image");

var child = builder.AddContainer("child", "child-image")
    .WithChildRelationship(parent);

await builder.Build().RunAsync();
```

### Deployment state management

Aspire 13.0 introduces deployment state management for persisting deployment information across runs.

```csharp
// Deployment state is automatically managed by the runtime
var builder = DistributedApplication.CreateBuilder(args);

// State persists across deployments
var api = builder.AddProject<Projects.Api>("api");

await builder.Build().RunAsync();
```

Deployment state features:

- **IDeploymentStateManager**: Interface for state management
- **FileDeploymentStateManager**: File-based state storage
- **UserSecretsDeploymentStateManager**: User secrets integration
- **Optimistic concurrency**: Prevents conflicting state updates
- **Section-based storage**: Organize state into logical sections

This enables scenarios like remembering parameter values, tracking deployed resources, and maintaining deployment history.

### Container files management

Enhanced container file operations with better error handling and source tracking.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api")
    .WithContainerFiles("./config", "/app/config", continueOnError: false)
    .WithContainerFilesSource("./shared-configs");

await builder.Build().RunAsync();
```

Container files features:

- **ContinueOnError**: Control failure behavior for file copy operations
- **Source tracking**: ContainerFilesSourceAnnotation tracks file sources
- **PublishWithContainerFiles**: Share files between resources during publishing
- **IResourceWithContainerFiles**: Interface for resources supporting container files

### Event system

Aspire 13.0 replaces lifecycle hooks with a new eventing system for better composability and testability.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres")
    .OnResourceStopped(async (resource, evt, cancellationToken) =>
    {
        // Handle resource stopped event
        Console.WriteLine($"Database {resource.Name} stopped");
        await CleanupAsync(cancellationToken);
    });

await builder.Build().RunAsync();
```

Event system features:

- **IDistributedApplicationEventingSubscriber**: Subscribe to application events
- **ResourceStoppedEvent**: Triggered when resources stop
- **ResourceEndpointsAllocatedEvent**: Triggered when endpoints are allocated
- **Composable subscriptions**: Register multiple subscribers for the same event
- **Cancellation support**: Properly handle cancellation during event processing

### Other app model improvements

**Compute environment support (graduated from experimental)**:
- `WithComputeEnvironment` API is now stable (no longer marked as experimental)
- Deploy resources to specific compute environments without experimental warnings

**Resource exclusion from MCP**:
- `ExcludeFromMcp` extension to exclude specific resources from Model Context Protocol exposure
- Control which resources are visible to AI assistants and MCP clients

**Reference environment injection control**:
- `WithReferenceEnvironment` to control how environment variables are injected from references
- `ReferenceEnvironmentInjectionFlags` for fine-grained control over environment variable behavior

**Helper methods**:
- `TryCreateResourceBuilder` for safely attempting resource builder creation with failure handling
- Returns false instead of throwing when resource builder creation fails

## ⚠️ Breaking changes

### Removed APIs

The following APIs have been removed in Aspire 13.0:

**Publishing infrastructure** (replaced by pipeline system):
- `PublishingContext` and `PublishingCallbackAnnotation`
- `DeployingContext` and `DeployingCallbackAnnotation`
- `WithPublishingCallback` extension method
- `IDistributedApplicationPublisher` interface
- `IPublishingActivityReporter`, `IPublishingStep`, `IPublishingTask` interfaces
- `NullPublishingActivityReporter` class
- `PublishingExtensions` class (all extension methods)
- `PublishingOptions` class

**Lifecycle hooks** (replaced by eventing system):
- `IDistributedApplicationLifecycleHook` interface

**Debugging APIs** (replaced with new flexible API):
- Old `WithDebugSupport` overload with `debugAdapterId` and `requiredExtensionId` parameters
- `SupportsDebuggingAnnotation` (replaced with new debug support annotation)

**Diagnostic codes**:
- `ASPIRECOMPUTE001` diagnostics (removed - API graduated from experimental)
- `ASPIREPUBLISHERS001` (renamed to `ASPIREPIPELINES001-003`)

### Changed signatures

**AllocatedEndpoint constructor**:
```csharp
// Before (9.x)
var endpoint = new AllocatedEndpoint("http", 8080, containerHostAddress: "localhost");

// After (13.0)
var endpoint = new AllocatedEndpoint("http", 8080, networkIdentifier: NetworkIdentifier.Host);
```

**ParameterProcessor constructor**:
```csharp
// Before (9.x)
var processor = new ParameterProcessor(distributedApplicationOptions);

// After (13.0)
var processor = new ParameterProcessor(deploymentStateManager);
```

**InteractionInput property changes**:
- `MaxLength`: Changed from settable to init-only
- `Options`: Changed from init-only to settable
- `Placeholder`: Changed from settable to init-only

**WithReference for IResourceWithServiceDiscovery**:
- Added new overload with `name` parameter for named references
- Existing overload still available for compatibility

**ProcessArgumentValuesAsync and ProcessEnvironmentVariableValuesAsync**:
```csharp
// Before (9.x)
await resource.ProcessArgumentValuesAsync(
    executionContext, processValue, logger,
    containerHostName: "localhost", cancellationToken);

// After (13.0) - uses NetworkIdentifier instead
await resource.ProcessArgumentValuesAsync(
    executionContext, processValue, logger, cancellationToken);
```

The `containerHostName` parameter has been removed from these extension methods. Network context is now handled through the `NetworkIdentifier` type.

### Migration guide

#### Migrating from publishing callbacks to pipeline steps

**Before (9.x)**:
```csharp
var api = builder.AddProject<Projects.Api>("api")
    .WithPublishingCallback(async (context, cancellationToken) =>
    {
        // Custom publishing logic
        await CustomDeployAsync(context, cancellationToken);
    });
```

**After (13.0)**:
```csharp
// Define a custom pipeline step
public class CustomDeployStep : PipelineStep
{
    public override async Task ExecuteAsync(PipelineStepContext context, CancellationToken cancellationToken)
    {
        // Custom deployment logic
        await CustomDeployAsync(context, cancellationToken);
    }
}

// Register the step
var api = builder.AddProject<Projects.Api>("api");
builder.Services.AddSingleton<CustomDeployStep>();
```

For more details on the pipeline system, see [Deployment pipeline documentation](../deployment/pipeline-architecture.md).

#### Migrating from lifecycle hooks to events

**Before (9.x)**:
```csharp
public class MyLifecycleHook : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        // Logic before start
    }
}

builder.Services.AddSingleton<IDistributedApplicationLifecycleHook, MyLifecycleHook>();
```

**After (13.0)**:
```csharp
public class MyEventSubscriber : IDistributedApplicationEventingSubscriber
{
    public async Task OnEventAsync<TEvent>(TEvent evt, CancellationToken cancellationToken)
        where TEvent : IDistributedApplicationEvent
    {
        if (evt is BeforeStartEvent beforeStart)
        {
            // Logic before start
        }
    }
}

builder.Services.AddSingleton<IDistributedApplicationEventingSubscriber, MyEventSubscriber>();
```

### Experimental features

The following features are marked as `[Experimental]` and may change in future releases:

- **Dockerfile builder API**: `WithDockerfileBuilder`, `AddDockerfileBuilder`, `WithDockerfileBaseImage`
- **C# app support**: `AddCSharpApp`
- **Dynamic inputs**: `InputLoadOptions`, dynamic input loading
- **Pipeline features**: `IDistributedApplicationPipeline` and related APIs

To use experimental features, you must enable them explicitly and acknowledge they may change:

```csharp
#pragma warning disable ASPIREXXX // Experimental feature
var app = builder.AddCSharpApp("myapp", "./app.cs");
#pragma warning restore ASPIREXXX
```

---

**Feedback and contributions**: We'd love to hear about your experience with Aspire 13.0! Share feedback on [GitHub](https://github.com/dotnet/aspire/issues) or join the conversation on [Discord](https://aka.ms/aspire-discord).
