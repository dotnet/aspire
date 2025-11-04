---
title: What's new in Aspire 13.0
description: Learn what's new in Aspire 13.0.
ms.date: 11/03/2025
---

# What's new in Aspire 13.0

📢 **Aspire 13 represents a major milestone in the Aspire product line.** Aspire is no longer ".NET Aspire" - it's now simply **Aspire**, a full **polyglot cloud-native application platform**. While Aspire continues to provide best-in-class support for .NET applications, version 13.0 elevates **Python and JavaScript to first-class citizens**, with comprehensive support for running, debugging, and deploying applications written in these languages.

This release introduces:
- **First-class Python support**: Debug Python modules in VS Code, deploy with uvicorn, use modern tooling like uv, and generate production Dockerfiles automatically
- **First-class JavaScript support**: Vite and npm-based apps with package manager auto-detection, debugging support, and container-based build pipelines
- **Polyglot infrastructure**: Connection properties work in any language (URI, JDBC, individual properties), certificate trust across languages and containers
- **Container files as build artifacts**: A new paradigm where build outputs are containers, not folders - enabling reproducible, isolated, and portable builds
- **aspire do: a new platform for build, publish and deployment pipelines**: enabling parallel execution, dependency tracking, and extensible workflows for building, publishing, and deploying applications
- **Modern CLI**: `aspire init` to Aspirify existing apps, and improved deployment state management that remembers your configuration across runs

Along with the rebranding, Aspire now has a new home at **[aspire.dev](https://aspire.dev)** - your central hub for documentation, getting started guides, and community resources.

**Requirements:**
- .NET 10 SDK or later

If you have feedback, questions, or want to contribute to Aspire, collaborate with us on [:::image type="icon" source="../media/github-mark.svg" border="false"::: GitHub](https://github.com/dotnet/aspire) or join us on [:::image type="icon" source="../media/discord-icon.svg" border="false"::: Discord](https://aka.ms/aspire-discord) to chat with the team and other community members.

## Table of contents

- [Upgrade to Aspire 13.0](#upgrade-to-aspire-130)
- [Aspire as a Polyglot Platform](#🌐-aspire-as-a-polyglot-platform)
  - [Python as a First-Class Citizen](#python-as-a-first-class-citizen)
    - [Flexible Python Application Models](#flexible-python-application-models)
    - [Uvicorn Integration for ASGI Applications](#uvicorn-integration-for-asgi-applications)
    - [Modern Python Tooling with uv](#modern-python-tooling-with-uv)
    - [VS Code Debugging Support](#vs-code-debugging-support)
    - [Automatic Dockerfile Generation](#automatic-dockerfile-generation)
    - [Python Version Detection](#python-version-detection)
    - [Starter Template: Vite + FastAPI](#starter-template-vite--fastapi)
  - [JavaScript as a First-Class Citizen](#javascript-as-a-first-class-citizen)
    - [Unified JavaScript Application Model](#unified-javascript-application-model)
    - [Package Manager Flexibility](#package-manager-flexibility)
    - [Customizing Scripts](#customizing-scripts)
    - [Vite Support](#vite-support)
    - [Dynamic Dockerfile Generation](#dynamic-dockerfile-generation)
  - [Polyglot Infrastructure](#polyglot-infrastructure)
    - [Polyglot Connection Properties](#polyglot-connection-properties)
    - [Certificate Trust Across Languages](#certificate-trust-across-languages)
    - [Simplified Service URL Environment Variables](#simplified-service-url-environment-variables)
- [CLI and Tooling](#🛠️-cli-and-tooling)
  - [aspire init command](#aspire-init-command)
  - [aspire update improvements](#aspire-update-improvements)
  - [Single-file AppHost support](#single-file-apphost-support)
  - [Automatic .NET SDK installation](#automatic-net-sdk-installation-preview)
  - [Non-interactive mode for CI/CD](#non-interactive-mode-for-cicd)
- [Major New Features](#⭐-major-new-features)
  - [aspire do](#aspire-do)
    - [Running pipeline steps](#running-pipeline-steps)
    - [Container Files as Build Artifacts](#container-files-as-build-artifacts-1)
  - [Dockerfile Builder API](#dockerfile-builder-api-experimental)
  - [Certificate Management](#certificate-management)
- [Integrations](#📦-integrations)
  - [.NET MAUI Integration](#net-maui-integration)
- [Dashboard Enhancements](#📊-dashboard-enhancements)
  - [Aspire MCP server](#aspire-mcp-server)
  - [Trace and telemetry improvements](#trace-and-telemetry-improvements)
  - [UI and accessibility improvements](#ui-and-accessibility-improvements)
- [App Model Enhancements](#🖥️-app-model-enhancements)
  - [C# app support](#c-app-support)
  - [Network identifiers](#network-identifiers)
  - [Dynamic input system](#dynamic-input-system-experimental)
  - [Reference and connection improvements](#reference-and-connection-improvements)
  - [Event system](#event-system)
  - [Other app model improvements](#other-app-model-improvements)
- [Deployment Improvements](#🚀-deployment-improvements)
  - [Deployment pipeline reimplementation](#deployment-pipeline-reimplementation)
  - [Deployment state management](#deployment-state-management)
- [Azure](#☁️-azure)
  - [Azure tenant selection](#azure-tenant-selection)
  - [Azure App Service enhancements](#azure-app-service-enhancements)
- [Breaking Changes](#⚠️-breaking-changes)

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

## 🌐 Aspire as a Polyglot Platform

Aspire 13 marks a transformative shift from a .NET-centric orchestration tool to a truly **polyglot cloud-native application platform**. Python and JavaScript are now first-class citizens alongside .NET, with comprehensive support for development, debugging, and deployment workflows.

### Python as a First-Class Citizen

Aspire 13 introduces comprehensive Python support, making it effortless to build, debug, and deploy Python applications alongside your other services.

#### Flexible Python Application Models

Aspire provides three ways to run Python code, each suited to different use cases:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Run a Python script directly
var script = builder.AddPythonScript("data-processor", "./scripts", "process.py")
    .WithReference(database);

// Run a Python module (python -m module_name)
var worker = builder.AddPythonModule("worker", "./worker", "worker.main")
    .WithReference(queue);

// Run any Python executable (e.g., Flask, FastAPI, uvicorn)
var api = builder.AddPythonExecutable("api", "./api", "uvicorn", ["main:app", "--reload"]);
```

#### Uvicorn Integration for ASGI Applications

For Python web applications using ASGI frameworks like FastAPI, Starlette, or Quart, Aspire provides dedicated `AddUvicornApp` support:

```csharp
var api = builder.AddUvicornApp("api", "./api", "main:app")
    .WithUvEnvironment()  // Use uv for fast, modern Python package management
    .WithExternalHttpEndpoints()
    .WithReference(database)
    .WithHttpHealthCheck("/health");
```

The `AddUvicornApp` method automatically:
- Configures HTTP/HTTPS endpoints
- Sets up appropriate Uvicorn command-line arguments
- Supports hot-reload during development
- Integrates with Aspire's health check system

#### Modern Python Tooling with uv

Aspire 13 integrates with [uv](https://github.com/astral-sh/uv), the modern Python package and project manager:

```csharp
builder.AddUvicornApp("api", "./api", "main:app")
    .WithUvEnvironment();  // Automatically uses uv for package management
```

When using `WithUvEnvironment()`, Aspire:
- Uses uv for fast, reliable dependency resolution
- Automatically syncs dependencies from `pyproject.toml`
- Creates isolated virtual environments per project
- Leverages uv's performance benefits (10-100x faster than pip)

#### VS Code Debugging Support

Python applications can be debugged directly in VS Code with full breakpoint support. Aspire 13 automatically enables debugging infrastructure for all Python resources.

**Automatic debugging configuration:**

Debugging support is automatically enabled for Python resources created with `AddPythonScript`, `AddPythonModule`, and `AddPythonExecutable`. No additional configuration is required:

```csharp
// Debugging is automatically enabled
var worker = builder.AddPythonModule("worker", "./worker", "worker.main");
// Internally calls .WithDebugging() automatically
```

**Supported debugging scenarios:**

- **Python scripts**: Debug `AddPythonScript` resources with breakpoints and variable inspection
- **Python modules**: Debug `AddPythonModule` with proper module resolution and import handling
- **Flask applications**: Debug Flask apps with auto-reload and request debugging
- **Uvicorn/FastAPI**: Debug ASGI applications with hot-reload and async/await support

**How it works:**

1. Aspire automatically configures Python debugging annotations for each Python resource
2. The Aspire IDE extension (VS Code) reads these annotations and generates launch configurations
3. Launch configurations are written to `.vscode/launch.json` with correct:
   - Python interpreter paths
   - Environment variables
   - Working directories
   - Module paths and entry points
4. The debugger attaches using `debugpy` (automatically installed in your virtual environment)

**IDE execution specifications:**

Aspire writes IDE execution specifications to `.aspire/ide-execution-spec.json` that include:
- Interpreter paths (`interpreterPath`)
- Module names for `-m` execution
- Environment variables for debugging
- Working directory paths

This enables seamless debugging across Python scripts, modules, Flask apps, and ASGI frameworks without manual configuration.

#### Automatic Dockerfile Generation

Aspire automatically generates production-ready Dockerfiles for Python applications when publishing. No additional configuration is required:

```csharp
builder.AddUvicornApp("api", "./api", "main:app")
    .WithUvEnvironment();
```

When you publish this app, Aspire automatically generates a Dockerfile that:
- Uses appropriate Python base images
- Installs dependencies using uv or pip
- Configures the working directory
- Sets up the ASGI server with production settings
- Follows Python container best practices

#### Python Version Detection

Aspire automatically detects the Python version for Dockerfile generation using multiple sources:

1. **`.python-version` file** (highest priority)
   ```
   3.13
   ```

2. **`pyproject.toml`** - `requires-python` field
   ```toml
   [project]
   requires-python = ">=3.13"
   ```

3. **Virtual environment** - Executes `python --version` as fallback

The detected version is used to select the appropriate Python base image for Docker publishing. Aspire does not enforce a minimum Python version requirement - any Python version detected through these methods will be supported.

#### Starter Template: Vite + FastAPI

Aspire 13 includes a new `aspire-py-starter` template that demonstrates a full-stack Python application:

```bash
aspire new aspire-py-starter
```

This template includes:
- **FastAPI backend**: Python ASGI application using Uvicorn
- **Vite + React frontend**: Modern JavaScript frontend with TypeScript
- **OpenTelemetry integration**: Distributed tracing across Python and JavaScript
- **Redis caching** (optional): Shared cache between services
- **Container files**: Frontend static files served by the Python backend

The template demonstrates how to build polyglot applications with Python and JavaScript working together seamlessly.

### JavaScript as a First-Class Citizen

Aspire 13 refactors and expands JavaScript support, introducing `AddJavaScriptApp` as the foundational method for all JavaScript applications.

#### Unified JavaScript Application Model

The new `AddJavaScriptApp` method replaces the older `AddNpmApp` (now obsolete) and provides a consistent way to add JavaScript applications:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a JavaScript application - runs "npm run dev" by default
var frontend = builder.AddJavaScriptApp("frontend", "./frontend");

// Use a different package manager
var admin = builder.AddJavaScriptApp("admin", "./admin")
    .WithYarn();
```

By default, `AddJavaScriptApp`:
- Automatically detects npm from `package.json`
- Runs the "dev" script during local development
- Runs the "build" script when publishing to create production assets

#### Package Manager Flexibility

Aspire automatically detects and supports multiple JavaScript package managers with intelligent defaults for both development and production scenarios.

**Auto-install by default:**

Starting in Aspire 13.0, package managers automatically install dependencies by default (`install = true`). This ensures dependencies are always up-to-date during development and publishing.

**Smart defaults for deterministic builds:**

When publishing (production mode), Aspire automatically uses deterministic install commands based on the presence of lockfiles:

- **npm**: Uses `npm ci` if `package-lock.json` exists, otherwise `npm install`
- **yarn**: Uses `yarn install --immutable` if `yarn.lock` exists, otherwise `yarn install`
- **pnpm**: Uses `pnpm install --frozen-lockfile` if `pnpm-lock.yaml` exists, otherwise `pnpm install`

This ensures reproducible builds in CI/CD and production environments while remaining flexible during development.

**Customizing package managers:**

```csharp
// Disable auto-install (not recommended)
var app1 = builder.AddJavaScriptApp("app1", "./app1")
    .WithNpm(install: false);

// Customize install command for npm
var app2 = builder.AddJavaScriptApp("app2", "./app2")
    .WithNpm(installCommand: "ci", installArgs: ["--legacy-peer-deps"]);

// Use yarn with custom arguments
var app3 = builder.AddJavaScriptApp("app3", "./app3")
    .WithYarn(installArgs: ["--immutable", "--check-cache"]);

// Use pnpm with specific flags
var app4 = builder.AddJavaScriptApp("app4", "./app4")
    .WithPnpm(installArgs: ["--frozen-lockfile", "--prefer-offline"]);
```

#### Customizing Scripts

You can customize which scripts run during development and build:

```csharp
// Use different script names
var app = builder.AddJavaScriptApp("app", "./app")
    .WithRunScript("start")      // Run "npm run start" during development instead of "dev"
    .WithBuildScript("prod");     // Run "npm run prod" during publish instead of "build"
```

#### Vite Support

`AddViteApp` is now a specialization of `AddJavaScriptApp` with Vite-specific optimizations:

```csharp
var frontend = builder.AddViteApp("frontend", "./frontend")
    .WithReference(api);
```

Vite applications get:
- Automatic port binding configuration
- Hot module replacement (HMR) support
- Optimized build scripts for production
- Automatic Dockerfile generation

#### Dynamic Dockerfile Generation

JavaScript applications automatically generate Dockerfiles when published, with intelligent defaults based on your package manager:

```csharp
var app = builder.AddJavaScriptApp("app", "./app");
```

The generated Dockerfile:
- Detects Node.js version from `.nvmrc`, `.node-version`, `package.json`, or `.tool-versions`
- Uses multi-stage builds for smaller images
- Installs dependencies in a separate layer for better caching
- Runs your build script to create production assets
- Defaults to `node:22-slim` if no version is specified

For information about using JavaScript build artifacts in other containers, see [Container Files as Build Artifacts](#container-files-as-build-artifacts).

### Polyglot Infrastructure

Beyond language-specific support, Aspire 13 introduces infrastructure features that work across all languages.

#### Polyglot Connection Properties

Database resources now expose multiple connection string formats automatically, making it easy to connect from any language:

```csharp
var postgres = builder.AddPostgres("db").AddDatabase("mydb");

// .NET app uses service discovery
var dotnetApi = builder.AddProject<Projects.Api>()
    .WithReference(postgres);

// Python app can use URI format
var pythonWorker = builder.AddPythonModule("worker", "./worker", "worker.main")
    .WithEnvironment("DATABASE_URL", postgres.Resource.UriExpression);

// Java app can use JDBC format
var javaApp = builder.AddExecutable("java-app", "java", "./app", ["-jar", "app.jar"])
    .WithEnvironment("DB_JDBC", postgres.Resource.JdbcConnectionStringExpression);
```

When you reference a database resource with `WithReference`, Aspire automatically exposes multiple connection properties as environment variables:

**PostgreSQL example** - for a resource named `db`, Aspire exposes:
- `DB_URI` - PostgreSQL URI format: `postgresql://user:pass@host:port/dbname`
- `DB_JDBCCONNECTIONSTRING` - JDBC format: `jdbc:postgresql://host:port/dbname?user=user&password=pass`
- `DB_HOST`, `DB_PORT`, `DB_USERNAME`, `DB_PASSWORD`, `DB_DATABASENAME` - Individual properties

**SQL Server example** - for a resource named `sql`, Aspire exposes:
- `SQL_URI` - SQL Server URI format: `mssql://user:pass@host:port/dbname`
- `SQL_JDBCCONNECTIONSTRING` - JDBC format: `jdbc:sqlserver://host:port;user=user;password=pass;databaseName=dbname;trustServerCertificate=true`
- `SQL_HOST`, `SQL_PORT`, `SQL_USERNAME`, `SQL_PASSWORD`, `SQL_DATABASENAME` - Individual properties

**Oracle example** - for a resource named `oracle`, Aspire exposes:
- `ORACLE_URI` - Oracle URI format: `oracle://user:pass@host:port/dbname`
- `ORACLE_JDBCCONNECTIONSTRING` - JDBC format: `jdbc:oracle:thin:user/pass@//host:port/dbname`
- `ORACLE_HOST`, `ORACLE_PORT`, `ORACLE_USERNAME`, `ORACLE_PASSWORD`, `ORACLE_DATABASE` - Individual properties

This works automatically for all supported databases including PostgreSQL, SQL Server, Oracle, MySQL, MongoDB, and more. No additional configuration needed - just use `WithReference` and access the connection format your language needs.

> [!NOTE]
> These new connection property conventions are available in the built-in Aspire database integrations (PostgreSQL, SQL Server, Oracle, MySQL, MongoDB, etc.). If you have custom or community integrations, they may need to be updated to expose these properties. See the [connection properties agent documentation](https://github.com/dotnet/aspire/blob/main/.github/agents/connectionproperties.agent.md) for guidance on implementing these conventions in your own integrations.

#### Certificate Trust Across Languages

Aspire 13 automatically configures certificate trust for Python, Node.js, and containerized applications without any additional configuration:

```csharp
// Python applications automatically trust development certificates
var pythonApi = builder.AddUvicornApp("api", "./api", "main:app");

// Node.js applications automatically trust development certificates
var nodeApi = builder.AddJavaScriptApp("frontend", "./frontend");

// Containerized applications automatically trust development certificates
var container = builder.AddContainer("service", "myimage");
```

Aspire automatically:
- **Python**: Configures `SSL_CERT_FILE` and `REQUESTS_CA_BUNDLE` environment variables
- **Node.js**: Configures `NODE_EXTRA_CA_CERTS` environment variable
- **Containers**: Mounts certificate bundles and configures appropriate environment variables
- **All platforms**: Generates and manages development certificates without manual intervention

This enables secure HTTPS communication during local development across all languages and containerized services.

#### Simplified Service URL Environment Variables

Aspire 13.0 introduces polyglot-friendly environment variables that make service discovery easier for non-.NET applications.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");

// Python app gets simple environment variables
var pythonApp = builder.AddPythonModule("worker", "./worker", "worker.main")
    .WithReference(api); // Sets API and API_HTTPS env vars

await builder.Build().RunAsync();
```

Instead of complex service discovery formats, non-.NET apps receive simple environment variables:

- `API=http://localhost:5000` - HTTP endpoint
- `API_HTTPS=https://localhost:5001` - HTTPS endpoint

This can be customized per-resource or per-type using `WithReferenceEnvironment()`:

```csharp
var api = builder.AddProject<Projects.Api>("api");

var nodeApp = builder.AddJavaScriptApp("frontend", "./frontend")
    .WithReference(api, env =>
    {
        // Customize environment variable generation
        env.EnvironmentVariables["API_URL"] = api.GetEndpoint("http");
    });
```

This feature makes Aspire's service discovery mechanism accessible to any programming language, not just .NET applications with service discovery libraries.

## 🛠️ CLI and Tooling

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

> [!NOTE]
> The `aspire init` command sets up the Aspire project structure and configuration, but does not automatically add resources (databases, caches, message queues, etc.) to your AppHost. You'll need to manually add resource definitions to your AppHost code using methods like `AddPostgres`, `AddRedis`, `AddRabbitMQ`, etc.

### aspire update improvements

The `aspire update` command has received significant improvements in Aspire 13.0, including the new `--self` flag to update the CLI itself:

```bash
# Update all Aspire packages in the current project
aspire update

# Update the Aspire CLI itself (new in 13.0)
aspire update --self

# Update a specific project
aspire update --project ./src/MyApp.AppHost
```

**New in Aspire 13.0:**
- **CLI self-update**: The `--self` flag allows you to update the Aspire CLI without reinstalling
- **Improved reliability**: Numerous bug fixes for edge cases in dependency resolution
- **Better error handling**: Clearer error messages when updates fail
- **Performance improvements**: Faster package detection and update operations

The `aspire update` command continues to support:
- Central package management (CPM) solutions
- Diamond dependency resolution
- Single-file app hosts
- XML fallback parsing for unresolvable AppHost SDKs
- Enhanced visual presentation with colorized output
- Channel awareness (stable, preview, staging)

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

### Non-interactive mode for CI/CD

Aspire 13.0 introduces the `--non-interactive` flag for automation-friendly output in CI/CD pipelines.

```bash
# Run commands without prompts or interactive elements
aspire deploy --non-interactive
aspire init --non-interactive
aspire update --non-interactive
```

When enabled, non-interactive mode disables user prompts and interactive progress indicators, providing clean output suitable for CI/CD logs. The CLI automatically detects common CI environments (GitHub Actions, Azure Pipelines, etc.) and enables this mode automatically.

**Environment variables:**

- `ASPIRE_NON_INTERACTIVE=true` - Enable non-interactive mode
- `NO_COLOR=1` - Disable ANSI colors in output

> [!NOTE]
> Not all commands support non-interactive mode. Commands that require user input will fail if the `--non-interactive` flag is set and required values are not provided through other means.

---

**For advanced deployment workflows**, see [aspire do](#aspire-do), which introduces a comprehensive pipeline system for coordinating build, deployment, and publishing operations.

## ⭐ Major New Features

### aspire do

Aspire 13.0 introduces `aspire do` - a comprehensive system for coordinating build, deployment, and publishing operations. This new architecture provides a foundation for orchestrating complex deployment workflows with built-in support for step dependencies, parallel execution, and detailed progress reporting.

The `aspire do` system replaces the previous publishing infrastructure with a more flexible, extensible model that allows resource-specific deployment logic to be decentralized and composed into larger workflows.

> [!IMPORTANT]
> 🧪 **Early Preview**: The pipeline APIs are in early preview and marked as experimental. While these APIs may evolve based on feedback, we're confident this is the right direction as it enables much more flexible modeling of arbitrary build, publish, and deployment steps. The pipeline system provides the foundation for advanced deployment scenarios that weren't possible with the previous publishing model.

For basic CLI commands and tooling, see [CLI and tooling](#cli-and-tooling), which covers [aspire init](#aspire-init-command), [aspire update](#aspire-update-improvements), and [non-interactive mode](#non-interactive-mode-for-cicd).

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

var frontend = builder.AddJavaScriptApp("frontend", "../frontend")
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

- **Global steps**: Define custom pipeline steps with `builder.Pipeline.AddStep`
- **Resource steps**: Resources contribute steps via `WithPipelineStepFactory`
- **Dependency configuration**: Control step ordering with `WithPipelineConfiguration`
- **Parallel execution**: Steps run concurrently when dependencies allow
- **Built-in logging**: Use `context.Logger` to log step progress
- **CLI execution**: Run specific steps with `aspire do <step-name>`

#### Running pipeline steps

Once you've defined your pipeline steps using the APIs above, you can execute them through the CLI using `aspire do`. This command serves as the primary entry point for running pipeline steps, whether they're built-in steps like `build`, `publish`, and `deploy`, or custom steps you've defined in your AppHost.

The `aspire do` command understands the entire pipeline graph, automatically resolving dependencies and executing steps in the correct order. For example, when you run `aspire do deploy`, it will automatically run any prerequisite steps (like `build` and `publish`) before executing the deployment itself.

```bash
# Execute a specific pipeline step (e.g., deploy)
# This automatically runs all required steps: build → publish → deploy
aspire do deploy

# Execute with custom output path
aspire do publish --output-path ./artifacts

# Execute with specific environment
aspire do deploy --environment Production

# Execute with verbose logging
aspire do deploy --log-level debug

# Execute a custom step you defined (like the "validate" example above)
aspire do validate
```

The `aspire do` command provides fine-grained control over deployment workflows, allowing you to execute any step in your pipeline independently while ensuring all dependencies are satisfied.

For more details on the pipeline architecture, see [Deployment pipeline documentation](../deployment/pipeline-architecture.md).

### Container Files as Build Artifacts

Aspire 13 introduces the ability to **extract files from one resource's container and copy them into another resource's container** during the build process. This enables powerful patterns like building a frontend in one container and serving it from a backend in another container.

```csharp
var frontend = builder.AddViteApp("frontend", "./frontend");

var api = builder.AddUvicornApp("api", "./api", "main:app");

// Extract files FROM the frontend container and copy TO the api container
api.PublishWithContainerFiles(frontend, "./static");
```

**How it works:**

1. The `frontend` resource builds inside its container, producing output files at `/app/dist`
2. Aspire extracts those files from the frontend container
3. The files are copied into the `api` container at `./static` during the build process
4. The final `api` container contains both the API code and the frontend static files

#### Example: Frontend with Backend API

A common pattern is building a frontend and serving it from a backend:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Build a Vite frontend in a container
var frontend = builder.AddViteApp("frontend", "./frontend");

// Python FastAPI backend
var api = builder.AddUvicornApp("api", "./api", "main:app")
    .WithUvEnvironment()
    .WithExternalHttpEndpoints();

// Extract frontend's /app/dist and copy to api's ./static
api.PublishWithContainerFiles(frontend, "./static");

builder.Build().Run();
```

When you deploy this:

1. **Frontend container builds**: Vite builds the React/Vue/Svelte app inside a Node container
2. **Files are extracted**: Aspire extracts `/app/dist` from the frontend container
3. **Files are injected**: The dist files are copied into the API container at `./static`
4. **Single deployment artifact**: The API container now contains both the Python app AND the frontend static files

The FastAPI app can serve the static files:

```python
from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles

app = FastAPI()

# Serve the frontend static files
app.mount("/", StaticFiles(directory="static", html=True), name="static")

# API endpoints
@app.get("/api/data")
def get_data():
    return {"message": "Hello from API"}
```

#### Using with .NET Projects

You can also use container files with .NET projects that produce build artifacts:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// .NET Blazor WebAssembly app that builds to wwwroot
var blazorWasm = builder.AddProject<Projects.BlazorWasm>("blazor-wasm");

// .NET API that serves static files
var api = builder.AddProject<Projects.Api>("api");

// Copy Blazor's published wwwroot into the API container
api.PublishWithContainerFiles(blazorWasm, "./static");
```

#### Container Files in the Pipeline

Container files integrate seamlessly with `aspire do`:

- **Dependency tracking**: The pipeline knows that the API container depends on the frontend container
- **Parallel execution**: Independent containers build in parallel
- **Incremental builds**: Only changed containers rebuild

This makes container files a natural fit for complex build workflows with multiple dependent services.

The `PublishWithContainerFiles` API is the key to this functionality, allowing you to specify which resource's container to extract files from and where to place them in the consuming container.

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
        StoreLocation.LocalMachine);

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
    .WithDeveloperCertificateTrust(trust: true); // Trust dev certs in container

await builder.Build().RunAsync();
```

Certificate management features include:

- **Multiple certificate sources**: Load from PEM files, Windows certificate stores, or programmatically
- **Flexible trust scoping**: System-level, append, override, or no trust
- **Container certificate paths**: Customize where certificates are placed in containers
- **Developer certificate support**: Automatic trust configuration for local development
- **Environment variable control**: Configure certificate behavior through environment variables

These features enable production-ready certificate handling in development, testing, and deployment scenarios.

## 📦 Integrations

Aspire 13.0 introduces new integration packages that expand platform support.

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

## 📊 Dashboard Enhancements

### Aspire MCP server

The Dashboard now includes an MCP server that integrates Aspire into your AI development ecosystem. The MCP server enables AI assistants to query resources, access telemetry data, and execute commands directly from your development environment.

**Capabilities:**

- **Resource monitoring**: Query real-time resource states, endpoints, and health status
- **Console logs**: Access console output for individual resources
- **Telemetry access**: Retrieve structured logs and distributed traces
- **Command execution**: Run resource commands through AI assistants
- **Privacy control**: Exclude sensitive resources from MCP using `ExcludeFromMcp()` annotation

**Getting started:**

1. Run your Aspire app and open the dashboard
2. Click the MCP button in the top right corner
3. Follow the instructions to configure your AI assistant (Claude Code, GitHub Copilot CLI, Cursor, VS Code, etc.)

The MCP server uses streamable HTTP with API key authentication for secure access. Configuration requires:
- `url`: The Aspire MCP endpoint address
- `type`: Set to "http" for the streamable-HTTP MCP server
- `x-mcp-api-key`: HTTP header for authentication

**Available tools:**

- `list_resources` - Retrieve all resources with state and metadata
- `list_console_logs` - Access resource console output
- `list_structured_logs` - Retrieve telemetry data, optionally filtered by resource
- `list_traces` - Access distributed trace information
- `list_trace_structured_logs` - View logs associated with specific traces
- `execute_resource_command` - Execute commands on resources

This enables AI assistants to directly interact with your Aspire applications, analyze telemetry in real-time, and provide intelligent insights during development.

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

## 🖥️ App Model Enhancements

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
var localhostEndpoint = api.GetEndpoint("http", KnownNetworkIdentifiers.LocalhostNetwork);
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

Reference resources with explicit names to control the environment variable prefix:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var primaryDb = builder.AddPostgres("postgres-primary")
    .AddDatabase("customers");

var replicaDb = builder.AddPostgres("postgres-replica")
    .AddDatabase("customers");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(primaryDb, "primary")
    .WithReference(replicaDb, "replica");
```

**Environment variables injected into `api`:**
```bash
# From primaryDb with "primary" name
ConnectionStrings__primary=Host=postgres-primary;...

# From replicaDb with "replica" name
ConnectionStrings__replica=Host=postgres-replica;...
```

This allows the application to distinguish between multiple database connections using the custom names.

#### Connection properties

Access individual connection string components to build custom connection strings or configuration:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").AddDatabase("mydb");
var redis = builder.AddRedis("cache");

var worker = builder.AddProject<Projects.Worker>("worker")
    .WithEnvironment("DB_HOST", postgres.Resource.GetConnectionProperty("Host"))
    .WithEnvironment("DB_PORT", postgres.Resource.GetConnectionProperty("Port"))
    .WithEnvironment("DB_NAME", postgres.Resource.DatabaseName)
    .WithEnvironment("CACHE_HOST", redis.Resource.GetConnectionProperty("Host"))
    .WithEnvironment("CACHE_PORT", redis.Resource.GetConnectionProperty("Port"));
```

**Environment variables injected into `worker`:**
```bash
DB_HOST=postgres
DB_PORT=5432
DB_NAME=mydb
CACHE_HOST=cache
CACHE_PORT=6379
```

This is useful when your application needs individual connection components rather than a full connection string, or when building connection strings in formats Aspire doesn't provide natively.

#### Endpoint reference enhancements

Control how service URLs are resolved and injected with network-aware endpoint references:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api")
    .WithExternalHttpEndpoints();

var frontend = builder.AddJavaScriptApp("frontend", "./frontend")
    .WithEnvironment("API_URL", api.GetEndpoint("https"));
```

**Environment variables injected into `frontend`:**
```bash
# Default behavior - uses the external endpoint URL
API_URL=https://localhost:7123
```

For advanced scenarios, you can specify the network context:

```csharp
var worker = builder.AddProject<Projects.Worker>("worker")
    .WithEnvironment("API_URL", api.GetEndpoint("http", KnownNetworkIdentifiers.DefaultAspireContainerNetwork));
```

**Environment variables injected into `worker`:**
```bash
# Container network context - uses internal container address
API_URL=http://api:8080
```

This enables proper service-to-service communication whether the consumer is running on the host or in a container.

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

## 🚀 Deployment Improvements

### Deployment pipeline reimplementation

Aspire 13.0 completely reimplements the deployment workflow on top of [aspire do](#aspire-do). This architectural change transforms deployment from a monolithic operation into a composable set of discrete, parallelizable steps.

#### Maximum Parallelization

The new deployment pipeline automatically parallelizes independent operations. Here's a real execution graph from `aspire do diagnostics` for an Azure deployment:

```
aspire deploy

Execution order (14 total steps):
  [0] build-prereq | deploy-prereq (parallel)
  [1] build-fe | validate-azure-login (parallel)
  [2] build-static | create-provisioning-context (parallel)
  [3] provision-env
  [4] login-to-acr-env
  [5] push-static
  [6] provision-static-containerapp
  [7] print-static-summary | provision-azure-bicep-resources (parallel)
  [8] print-dashboard-url-env
  [9] deploy
```

Notice how the pipeline automatically parallelizes at multiple levels:
- **Level 0**: Prerequisites run in parallel
- **Level 1**: Frontend builds while Azure login validates (parallel)
- **Level 2**: Static files build while provisioning context is created (parallel)
- **Level 7**: Summary printing and Bicep resource provisioning run in parallel

This dramatically reduces deployment time for applications with multiple services by executing independent steps concurrently.

#### Granular Step Control

You can now execute individual deployment phases as discrete operations using `aspire do`:

```bash
# Build all containers
aspire do build

# Push a specific container image
aspire do push-static

# Provision Azure infrastructure
aspire do provision-azure-bicep-resources

# Deploy everything
aspire deploy
```

This granular control enables powerful workflows:

**Incremental deployments**: Build once, reuse across environments
```bash
aspire do build                              # Build containers locally
aspire do push-static                        # Push to registry
aspire do provision-azure-bicep-resources    # Deploy infrastructure
aspire deploy                                # Complete deployment
```

**Debugging builds**: Iterate on specific steps
```bash
aspire do build-fe        # Build just the frontend
aspire do build-static    # Build just the static files
aspire deploy             # Then deploy everything
```

**CI/CD integration**: Split pipeline stages
```bash
# CI stage: Build and test
aspire do build

# CD stage: Push and deploy
aspire deploy
```

#### Pipeline Diagnostics

Aspire 13.0 includes a built-in `aspire do diagnostics` command to help you understand and troubleshoot your pipeline graph:

```bash
aspire do diagnostics
```

This command provides comprehensive information about your pipeline:

**Execution order analysis:**
Shows the complete execution order with parallelization indicators:
```
Execution order (14 total steps):
  [0] build-prereq | deploy-prereq (parallel)
  [1] build-fe | validate-azure-login (parallel)
  [2] build-static | create-provisioning-context (parallel)
  ...
```

**Detailed step analysis:**
For each step, see:
- Dependencies (with validation)
- Associated resources
- Tags for categorization

```
Step: push-static
    Dependencies: ✓ build-static, ✓ login-to-acr-env, ✓ provision-env
    Resource: static-containerapp (AzureContainerAppResource)
    Tags: push-container-image
```

**"What If" simulation:**
See exactly what steps will run for any target:
```
If targeting 'build':
  Total steps: 5
  Execution order:
    [0] build-prereq | deploy-prereq (parallel)
    [1] build-fe
    [2] build-static
    [3] build
```

**Problem detection:**
Identifies configuration issues:
- Orphaned steps (not required by anything)
- Missing dependencies
- Circular dependencies

Use `aspire do diagnostics` when:
- Setting up a new deployment pipeline
- Adding custom pipeline steps
- Debugging why certain steps aren't running
- Understanding deployment performance

#### Pipeline Step Benefits

The pipeline-based deployment provides:

- **Dependency tracking**: Steps automatically run prerequisites
- **Progress reporting**: Real-time status for each step
- **Failure isolation**: Identify exactly which step failed
- **Selective execution**: Run only the steps you need
- **Extensibility**: Add custom pipeline steps via pipeline API
- **Built-in diagnostics**: `aspire do diagnostics` for pipeline visualization and troubleshooting

For more details on the underlying pipeline system, see [aspire do](#aspire-do).

### Deployment state management

Aspire 13.0 introduces deployment state management that automatically persists deployment information locally across runs. When you deploy to Azure, Aspire now remembers your choices and deployment state between sessions.

**What persists locally:**

- **Azure configuration**: Subscription, resource group, location, and tenant selections
- **Parameter values**: Input values from previous deployments
- **Deployed resources**: Track what's been deployed and where
- **Deployment context**: Maintain context across multiple deployment runs

**User experience:**

```bash
# First deployment - you're prompted for configuration
aspire deploy
# Select Azure subscription, resource group, location, tenant...

# Subsequent deployments - no prompts, uses saved state
aspire deploy
# Uses previous selections automatically
```

This eliminates repetitive prompts and makes iterative deployments faster. Your deployment configuration is stored locally (not in source control), so each developer can have their own Azure configuration without conflicts.

**Example workflow:**

1. First time: Select subscription "My Subscription", resource group "my-rg", location "eastus"
2. Deploy completes, state saved locally
3. Make code changes
4. Run `aspire deploy` again - automatically uses "My Subscription", "my-rg", "eastus"
5. No need to re-enter configuration

**Storage location:**

The state is stored in your local user profile at:

- **Windows**: `C:\Users\<username>\.aspire\deployments\<project-hash>\<environment>.json`
- **macOS/Linux**: `/Users/<username>/.aspire/deployments/<project-hash>/<environment>.json`

The `<project-hash>` is a SHA256 hash of your AppHost project path, allowing different projects to maintain separate state. The `<environment>` corresponds to the deployment environment (e.g., `production`, `development`).

This location is excluded from source control by design, so each developer can have their own Azure configuration without conflicts.

**Resetting deployment state:**

If you need to reset your deployment state (for example, to change subscriptions or start fresh), use the `--clear-cache` flag:

```bash
aspire deploy --clear-cache
# Clears saved state and prompts for configuration again
```

This removes all saved deployment state, including Azure configuration, parameter values, and deployment context. The next deployment will prompt you for all configuration values as if it were the first deployment.

## ☁️ Azure

### Azure tenant selection

Aspire 13.0 introduces interactive tenant selection during Azure provisioning, fixing issues with multi-tenant scenarios (work and personal accounts).

When provisioning Azure resources, if multiple tenants are available, the CLI will prompt you to select the appropriate tenant. The tenant selection is stored alongside your subscription, location, and resource group choices for consistent deployments.

```bash
aspire deploy

# If you have multiple tenants, you'll be prompted:
# Select Azure tenant:
#   > work@company.com (Default Directory)
#     personal@outlook.com (Personal Account)
```

### Azure App Service enhancements

Aspire 13.0 brings significant improvements to Azure App Service deployment, making it easier to deploy and monitor your applications in production.

#### Aspire Dashboard in App Service

The Aspire Dashboard is now included by default when deploying to Azure App Service, giving you visibility into your deployed applications:

```csharp
builder.AddAzureAppServiceEnvironment("env");
// Dashboard is included by default at https://[prefix]-aspire-dashboard.azurewebsites.net
```

The deployed dashboard provides the same experience as local development: view logs, traces, metrics, and application topology for your production environment.

To disable the dashboard:

```csharp
builder.AddAzureAppServiceEnvironment("env")
    .WithDashboard(enable: false);
```

#### Application Insights integration

Enable Azure Application Insights for comprehensive monitoring and telemetry:

```csharp
builder.AddAzureAppServiceEnvironment("env")
    .WithAzureApplicationInsights();
```

When enabled, Aspire automatically:
- Creates a Log Analytics workspace
- Creates an Application Insights resource
- Configures all App Service Web Apps with the connection string
- Injects `APPLICATIONINSIGHTS_CONNECTION_STRING` into your applications

You can also reference an existing Application Insights resource:

```csharp
var insights = builder.AddAzureApplicationInsights("insights");

builder.AddAzureAppServiceEnvironment("env")
    .WithAzureApplicationInsights(insights);
```

#### Automatic scaling

Enable automatic scaling for the App Service Plan to improve performance and avoid cold start issues:

```csharp
builder.AddAzureAppServiceEnvironment("env")
    .WithAutomaticScaling();
```

This enables elastic scale on the App Service Plan, capping at 10 workers following Azure best practices. Without automatic scaling, each app service scales independently with per-site scaling.

## ⚠️ Breaking Changes

### Removed APIs

The following APIs have been removed in Aspire 13.0:

**Publishing infrastructure** (replaced by `aspire do`):
- `PublishingContext` and `PublishingCallbackAnnotation`
- `DeployingContext` and `DeployingCallbackAnnotation`
- `WithPublishingCallback` extension method
- `IDistributedApplicationPublisher` interface
- `IPublishingActivityReporter`, `IPublishingStep`, `IPublishingTask` interfaces
- `NullPublishingActivityReporter` class
- `PublishingExtensions` class (all extension methods)
- `PublishingOptions` class
- `CompletionState` enum

**Lifecycle hooks** (replaced by eventing system):
- `IDistributedApplicationLifecycleHook` interface

**Debugging APIs** (replaced with new flexible API):
- Old `WithDebugSupport` overload with `debugAdapterId` and `requiredExtensionId` parameters
- `SupportsDebuggingAnnotation` (replaced with new debug support annotation)

**Diagnostic codes**:
- `ASPIRECOMPUTE001` diagnostics (removed - API graduated from experimental)
- `ASPIREPUBLISHERS001` (renamed to `ASPIREPIPELINES001-003`)

**CLI commands**:
- `--watch` flag removed from `aspire run` (replaced by `features.defaultWatchEnabled` feature flag)

### Obsolete APIs

The following APIs are marked as obsolete in Aspire 13.0 and will be removed in a future release:

**Lifecycle hook extension methods** (use eventing subscriber extensions instead):
- `AddLifecycleHook<T>()` - Use `AddEventingSubscriber<T>()` instead
- `AddLifecycleHook<T>(Func<IServiceProvider, T>)` - Use `AddEventingSubscriber<T>(Func<IServiceProvider, T>)` instead
- `TryAddLifecycleHook<T>()` - Use `TryAddEventingSubscriber<T>()` instead
- `TryAddLifecycleHook<T>(Func<IServiceProvider, T>)` - Use `TryAddEventingSubscriber<T>(Func<IServiceProvider, T>)` instead

**Publishing interfaces** (use `aspire do` instead):
- `IDistributedApplicationPublisher` - Use `PipelineStep` instead
- `PublishingOptions` - Use `PipelineOptions` instead

**Node.js/JavaScript APIs** (use new JavaScript hosting instead):
- `AddNpmApp()` - Use `AddJavaScriptApp()` instead for general npm-based apps, or `AddViteApp()` for Vite projects

While these APIs still work in 13.0, they will be removed in the next major version. Update your code to use the recommended replacements.

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

**EndpointReference.GetValueAsync behavior change**:
- `EndpointReference.GetValueAsync` now waits for endpoint allocation before resolving values
- Previously, it would throw immediately if the endpoint wasn't allocated
- Code that relied on immediate throwing will now hang unless `IsAllocated` is checked manually first

```csharp
// Before (9.x) - would throw immediately if not allocated
var value = await endpointRef.GetValueAsync(cancellationToken);

// After (13.0) - waits for allocation, check IsAllocated if you need immediate failure
if (!endpointRef.IsAllocated)
{
    throw new InvalidOperationException("Endpoint not allocated");
}
var value = await endpointRef.GetValueAsync(cancellationToken);
```

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

The `AddNodeApp` API has been refactored in Aspire 13.0 with breaking changes to how Node.js applications are configured.

**Signature changes:**

```csharp
// Before (9.x) - absolute scriptPath with optional workingDirectory
builder.AddNodeApp(
    name: "frontend",
    scriptPath: "/absolute/path/to/app.js",    // Absolute path to script
    workingDirectory: "/absolute/path/to",     // Optional working directory
    args: ["--port", "3000"]);

// After (13.0) - appDirectory with relative scriptPath
builder.AddNodeApp(
    name: "frontend",
    appDirectory: "../frontend",    // Directory containing the app
    scriptPath: "app.js");          // Relative path from appDirectory
```

**Behavioral changes:**

1. **Automatic npm integration**: If `package.json` exists in `appDirectory`, npm is automatically configured as the package manager with auto-install enabled
2. **Automatic Dockerfile generation**: The new API includes Docker publishing support with multi-stage builds by default
3. **Package manager flexibility**: Use `WithNpm()`, `WithYarn()`, or `WithPnpm()` combined with `WithRunScript()` to execute package.json scripts instead of direct node execution

**Migration example:**

```csharp
// Before (9.x)
var app = builder.AddNodeApp("frontend", "../frontend/server.js", "../frontend");

// After (13.0) - basic migration
var app = builder.AddNodeApp("frontend", "../frontend", "server.js");

// After (13.0) - with package.json script
var app = builder.AddNodeApp("frontend", "../frontend", "server.js")
    .WithNpm()
    .WithRunScript("dev");  // Runs "npm run dev" instead
```

**Impact:** If you're using `AddNodeApp` directly, update your method calls to use the new parameter structure. The old method signature is marked obsolete and will be removed in a future release.

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
