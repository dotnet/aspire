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
- [CLI and tooling](#-cli-and-tooling)
  - [aspire init command](#aspire-init-command)
  - [aspire update command](#aspire-update-command)
  - [aspire do command](#aspire-do-command-pipeline-entry-point)
  - [aspire cache command](#aspire-cache-command)
  - [Single-file AppHost support](#single-file-apphost-support)
  - [Automatic .NET SDK installation](#automatic-net-sdk-installation)
  - [Other CLI improvements](#other-cli-improvements)
- [Major new features](#major-new-features)
  - [Distributed Application Pipeline](#distributed-application-pipeline)
  - [Dashboard AI Assistant](#dashboard-ai-assistant)
  - [Dockerfile Builder API](#dockerfile-builder-api-experimental)
  - [Certificate Management](#certificate-management)
- [Integration Packages and Resources](#-integration-packages-and-resources)
  - [Vite App Support](#vite-app-support)
  - [.NET MAUI Integration](#net-maui-integration)
  - [Python Enhancements](#python-enhancements)
  - [Simplified Service URL Environment Variables](#simplified-service-url-environment-variables)
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
  - [Azure deployment enhancements](#azure-deployment-enhancements)
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
# Execute a specific pipeline step (e.g., deploy)
aspire do deploy

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

## Major new features

### Distributed Application Pipeline

Aspire 13.0 introduces a comprehensive pipeline system for coordinating build, deployment, and publishing operations. This new architecture provides a foundation for orchestrating complex deployment workflows with built-in support for step dependencies, parallel execution, and detailed progress reporting.

The pipeline system replaces the previous publishing infrastructure with a more flexible, extensible model that allows resource-specific deployment logic to be decentralized and composed into larger workflows.

> [!IMPORTANT]
> 🧪 **Early Preview**: The pipeline APIs are in early preview and marked as experimental. While these APIs may evolve based on feedback, we're confident this is the right direction as it enables much more flexible modeling of arbitrary build, publish, and deployment steps. The pipeline system provides the foundation for advanced deployment scenarios that weren't possible with the previous publishing model.

**Global pipeline steps:**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a custom pipeline step that runs before build
builder.Pipeline.AddStep("validate", (context) =>
{
    context.Logger.LogInformation("Running validation checks...");
    // Your custom validation logic
    context.Logger.LogInformation("Validation complete!");
    return Task.CompletedTask;
}, requiredBy: "build");

await builder.Build().RunAsync();
```

You can run this step directly using the CLI:

```bash
# Run the validate step and all its dependencies
aspire do validate
```

**Resource-specific pipeline steps:**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api")
    .WithPipelineStepFactory(context => new PipelineStep
    {
        Name = "seed-database",
        Action = async (ctx) =>
        {
            ctx.Logger.LogInformation("Seeding database for {Resource}...", context.Resource.Name);
            // Your seeding logic here
            await Task.CompletedTask;
        }
    });

await builder.Build().RunAsync();
```

**Configure step dependencies between resources:**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");

var frontend = builder.AddNpmApp("frontend", "../frontend")
    .WithPipelineConfiguration(context =>
    {
        // Get the build steps for this resource
        var frontendBuild = context.GetSteps(context.Resource, WellKnownPipelineTags.BuildCompute);

        // Get the build steps for the API resource
        var apiBuild = context.GetSteps(api.Resource, WellKnownPipelineTags.BuildCompute);
 
        // Make frontend build depend on API build
        frontendBuild.DependsOn(apiBuild);
    });

await builder.Build().RunAsync();
```

The pipeline system includes:

- **Global steps**: Define custom deployment steps with `builder.Pipeline.AddStep`
- **Resource steps**: Resources contribute steps via `WithPipelineStepFactory`
- **Dependency configuration**: Control step ordering with `WithPipelineConfiguration`
- **Parallel execution**: Steps run concurrently when dependencies allow
- **Built-in logging**: Use `context.Logger` to log step progress
- **CLI execution**: Run specific steps with `aspire do <step-name>`

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

var app = builder.AddContainer("goapp", "goapp")
    .PublishAsDockerFile(publish =>
    {
        publish.WithDockerfileBuilder("/path/to/goapp", context =>
        {
            // Build stage - compile Go application
            var buildStage = context.Builder
                .From("golang:1.23-alpine", "builder")
                .EmptyLine()
                .Comment("Install build dependencies")
                .Run("apk add --no-cache git")
                .EmptyLine()
                .WorkDir("/build")
                .Comment("Download dependencies first for better caching")
                .Copy("go.mod", "./")
                .Copy("go.sum", "./")
                .Run("go mod download")
                .EmptyLine()
                .Comment("Copy source and build")
                .Copy(".", "./")
                .Run("CGO_ENABLED=0 GOOS=linux go build -o /app/server .");

            // Runtime stage - minimal runtime image
            context.Builder
                .From("alpine:latest", "runtime")
                .EmptyLine()
                .Comment("Install CA certificates for HTTPS")
                .Run("apk add --no-cache ca-certificates")
                .EmptyLine()
                .Comment("Create non-root user")
                .Run("adduser -D -u 1000 appuser")
                .EmptyLine()
                .Comment("Copy binary from builder")
                .CopyFrom(buildStage.StageName!, "/app/server", "/app/server", "appuser:appuser")
                .EmptyLine()
                .User("appuser")
                .WorkDir("/app")
                .EmptyLine()
                .Entrypoint(["/app/server"]);
        });
    });

await builder.Build().RunAsync();
```

The Dockerfile Builder API provides:

- **Multi-stage builds**: Create stages with `From(image, stageName)` and reference them with `CopyFrom`
- **Fluent API**: Chain methods like `WorkDir`, `Copy`, `Run`, `Env`, `User`, `Entrypoint`
- **Comments and formatting**: Add comments and empty lines for readable generated Dockerfiles
- **BuildKit features**: Use `RunWithMounts` for cache mounts and bind mounts
- **Dynamic generation**: Access resource configuration via `context.Resource` to customize based on annotations

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

## 📦 Integration Packages and Resources

Aspire 13.0 introduces several new integration packages and resource types that expand support beyond traditional .NET scenarios.

### Vite App Support

Aspire 13.0 adds first-class support for Vite-based frontend applications with automatic package manager detection and Dockerfile generation.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a Vite app with automatic package manager detection
var frontend = builder.AddViteApp("frontend", "../frontend")
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

// Or specify the package manager explicitly
var frontendWithNpm = builder.AddViteApp("frontend-npm", "../frontend")
    .WithNpm() // or .WithYarn() or .WithPnpm()
    .WithHttpEndpoint(env: "PORT");

await builder.Build().RunAsync();
```

Vite app features include:

- **Automatic package manager detection**: Detects npm, yarn, or pnpm from lock files
- **Node.js version detection**: Automatically detects Node.js version from project configuration files
- **Dockerfile generation**: Automatic multi-stage Dockerfile for production builds
- **Package manager methods**: `WithNpm()`, `WithYarn()`, `WithPnpm()` for explicit package manager selection
- **Auto-install packages**: Automatically runs package install commands during startup

This feature was contributed from the Community Toolkit and is now part of core Aspire, making Vite (and similar modern JavaScript build tools) a first-class citizen alongside .NET projects.

### .NET MAUI Integration

Aspire 13.0 introduces a new `Aspire.Hosting.Maui` package that enables orchestrating .NET MAUI mobile applications alongside your cloud services.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");

// Add MAUI app for Windows
var mauiWindows = builder.AddMauiWindows("myapp-windows", "../MyApp/MyApp.csproj")
    .WithReference(api);

// Add MAUI app for Mac Catalyst
var mauiMac = builder.AddMauiMacCatalyst("myapp-mac", "../MyApp/MyApp.csproj")
    .WithReference(api);

await builder.Build().RunAsync();
```

MAUI integration features:

- **Platform support**: Windows and Mac Catalyst platforms
- **Device registration**: Register multiple device instances for testing
- **Platform validation**: Automatically detects host OS compatibility and marks resources as unsupported when needed
- **Full orchestration**: MAUI apps participate in service discovery and can reference backend services

This enables a complete mobile + cloud development experience where you can run and debug your mobile app alongside your backend services in a single Aspire project.

### Python Enhancements

#### Uvicorn App Resource

Aspire 13.0 introduces a specialized `UvicornAppResource` for Python ASGI applications, following the same pattern as `ViteAppResource`.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a Uvicorn app (FastAPI, Starlette, etc.)
var api = builder.AddUvicornApp("api", "../api", "main:app")
    .WithHttpEndpoint(port: 8000, env: "PORT")
    .WithExternalHttpEndpoints();

await builder.Build().RunAsync();
```

The `AddUvicornApp` method returns `IResourceBuilder<UvicornAppResource>` instead of the generic `PythonAppResource`, providing better type safety and enabling Uvicorn-specific extensions in the future.

#### Python Module Debugging

Aspire 13.0 adds comprehensive debugging support for Python modules (not just scripts), including popular frameworks.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Debug a Flask app
var flaskApp = builder.AddPythonModule("flask-app", "../app", "flask")
    .WithArgs("run", "--debug")
    .WithDebugging(); // Enables module debugging

// Debug a Uvicorn app
var uvicornApp = builder.AddPythonModule("api", "../api", "uvicorn")
    .WithArgs("main:app", "--reload")
    .WithDebugging(); // Enables module debugging

await builder.Build().RunAsync();
```

Python debugging enhancements:

- **Module debugging**: Full debugging support for `python -m module` execution
- **Framework support**: Pre-configured debugging for Flask, Uvicorn, and Gunicorn
- **IDE integration**: Enhanced IDE spec with `interpreter_path` and `module` properties
- **New method**: `AddGunicornApp()` for Gunicorn-based applications

### Simplified Service URL Environment Variables

Aspire 13.0 introduces polyglot-friendly environment variables that make service discovery easier for non-.NET applications.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");

// Python app gets simple environment variables
var pythonApp = builder.AddPythonApp("worker", "../worker", "app.py")
    .WithReference(api); // Sets PROJECTS_API and PROJECTS_API_HTTPS env vars

await builder.Build().RunAsync();
```

Instead of complex service discovery formats, non-.NET apps receive simple environment variables:

- `WEATHERAPI=http://localhost:5000` - HTTP endpoint
- `WEATHERAPI_HTTPS=https://localhost:5001` - HTTPS endpoint

This can be customized per-resource or per-type using `WithReferenceEnvironment()`:

```csharp
var api = builder.AddProject<Projects.Api>("api");

var nodeApp = builder.AddNpmApp("frontend", "../frontend")
    .WithReference(api, env =>
    {
        // Customize environment variable generation
        env.EnvironmentVariables["API_URL"] = api.GetEndpoint("http");
    });
```

This feature makes Aspire's service discovery mechanism accessible to any programming language, not just .NET applications with service discovery libraries.

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

### Azure deployment enhancements

#### Azure tenant selection

Aspire 13.0 introduces interactive tenant selection during Azure provisioning, fixing issues with multi-tenant scenarios (work and personal accounts).

When provisioning Azure resources, if multiple tenants are available, the CLI will prompt you to select the appropriate tenant. The tenant selection is stored alongside your subscription, location, and resource group choices for consistent deployments.

```bash
aspire deploy

# If you have multiple tenants, you'll be prompted:
# Select Azure tenant:
#   > work@company.com (Default Directory)
#     personal@outlook.com (Personal Account)
```

#### Azure Key Vault emulator support

Azure Key Vault hosting now supports local development with the Azure Key Vault emulator, eliminating the need for an Azure subscription during development.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Uses emulator in development, real Key Vault in production
var keyVault = builder.AddAzureKeyVault("keyvault");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(keyVault);

await builder.Build().RunAsync();
```

The emulator integration uses connection string redirect for local development scenarios.

#### Azure App Service automatic scaling

Enable automatic scaling for Azure App Service Plan to improve performance and avoid cold start issues in production.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureAppService("api")
    .WithAutomaticScaling(); // Enables automatic scaling

await builder.Build().RunAsync();
```

This is a best practice for production deployments to ensure your application scales appropriately with load.

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

### Major architectural changes

#### Universal Container-to-Host Communication

Aspire 13.0 introduces a major architectural change to enable universal container-to-host communication, independent of container orchestrator support.

**What changed:**
- Leverages DCP's container tunnel capability for container-to-host connectivity
- `EndpointReference` resolution is now context-aware (uses `NetworkIdentifier`)
- Endpoint references are tracked by their `EndpointAnnotation`
- `AllocatedEndpoint` constructor signature changed (see above)

**Impact:**
- This enables containers to communicate with host-based services reliably across all deployment scenarios
- Code that directly constructs `AllocatedEndpoint` objects will need updates
- Extension methods that process endpoint references may need Network Identifier context

**Migration:**
Most applications won't need changes as the endpoint resolution happens automatically. However, if you have custom code that creates or processes endpoints:

```csharp
// Before (9.x)
var endpoint = new AllocatedEndpoint("http", 8080, containerHostAddress: "localhost");

// After (13.0) - specify network context
var endpoint = new AllocatedEndpoint("http", 8080, networkIdentifier: NetworkIdentifier.Host);
```

This change fixes long-standing issues with container-to-host communication (issue #6547).

#### Refactored AddNodeApp API

The `AddNodeApp` API has been refactored in Aspire 13.0, introducing breaking changes to how Node.js applications are added.

**What changed:**
- Updated method signatures and behavior
- Package manager integration changes (npm/yarn/pnpm now auto-install by default)

**Impact:**
If you're using `AddNodeApp` directly, review your code for compatibility with the new API. The new Vite app support (`AddViteApp`) follows similar patterns.

For most users, the changes are improvements that reduce boilerplate, but may require minor code updates if you have custom Node.js integrations.

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
