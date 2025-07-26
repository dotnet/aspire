---
title: What's new in .NET Aspire 9.4
description: Learn what's new in the official general availability release of .NET Aspire 9.4.
ms.date: 07/25/2025
---

# What's new in .NET Aspire 9.4

📢 .NET Aspire 9.4 is the next minor version release of .NET Aspire. It supports:

- .NET 8.0 Long Term Support (LTS)
- .NET 9.0 Standard Term Support (STS)

If you have feedback, questions, or want to contribute to .NET Aspire, collaborate with us on [:::image type="icon" source="../media/github-mark.svg" border="false"::: GitHub](https://github.com/dotnet/aspire) or join us on [:::image type="icon" source="../media/discord-icon.svg" border="false"::: Discord](https://aka.ms/dotnet-discord) to chat with team members.

It's important to note that .NET Aspire releases out-of-band from .NET releases. While major versions of .NET Aspire align with major .NET versions, minor versions are released more frequently. For more information on .NET and .NET Aspire version support, see:

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy): Definitions for LTS and STS.
- [.NET Aspire support policy](https://dotnet.microsoft.com/platform/support/policy/aspire): Important unique product life cycle details.

## ⬆️ Upgrade to .NET Aspire 9.4

Moving between minor releases of .NET Aspire is simple:

1. In your app host project file (that is, _MyApp.AppHost.csproj_), update the [📦 Aspire.AppHost.Sdk](https://www.nuget.org/packages/Aspire.AppHost.Sdk) NuGet package to version `9.4.0`:

    ```xml
    <PackageReference Include="Aspire.AppHost.Sdk" Version="9.4.0" />
    ```

    For more information, see [.NET Aspire SDK](xref:dotnet/aspire/sdk).

1. Check for any NuGet package updates, either using the NuGet Package Manager in Visual Studio or the **Update NuGet Package** command in VS Code.
1. Update to the latest [.NET Aspire templates](../fundamentals/aspire-sdk-templates.md) by running the following .NET command line:

    ```dotnetcli
    dotnet new update
    ```

    > The `dotnet new update` command updates all of your templates to the latest version.

If your app host project file doesn't have the `Aspire.AppHost.Sdk` reference, you might still be using .NET Aspire 8. To upgrade to 9.0, follow [the upgrade guide](../get-started/upgrade-to-aspire-9.md).

## 🖥️ App model enhancements

### ✨ Advanced YARP routing with transform APIs

Building sophisticated reverse proxy configurations has traditionally required deep knowledge of YARP's transform system and manual JSON configuration. .NET Aspire 9.4 introduces a comprehensive set of fluent APIs that make advanced routing transformations accessible through strongly-typed C# code.

You can now programmatically configure request/response transformations, header manipulation, path rewriting, and query string handling directly from your app model—no more wrestling with complex configuration files.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var backendApi = builder.AddProject<Projects.BackendApi>("backend-api");
var identityService = builder.AddProject<Projects.Identity>("identity-service");

var yarp = builder.AddYarp("gateway")
    .WithConfiguration(yarpBuilder =>
    {
        // Configure sophisticated routing with transforms
        yarpBuilder.AddRoute("/api/v1/{**catch-all}", backendApi)
            .WithTransformPathPrefix("/v2")  // Rewrite /api/v1/* to /v2/*
            .WithTransformRequestHeader("X-API-Version", "2.0")
            .WithTransformForwarded(useHost: true, useProto: true)
            .WithTransformResponseHeader("X-Powered-By", "Aspire Gateway");

        // Advanced header and query manipulation
        yarpBuilder.AddRoute("/auth/{**catch-all}", identityService)
            .WithTransformClientCertHeader("X-Client-Cert")
            .WithTransformQueryValue("client_id", "aspire-app")
            .WithTransformRequestHeadersAllowed("Authorization", "Content-Type")
            .WithTransformUseOriginalHostHeader(false);
    });

builder.Build().Run();
```

This eliminates the need for complex YARP configuration files while providing complete access to YARP's powerful transformation pipeline. The fluent API makes it easy to implement common patterns like API versioning, header enrichment, and security transformations.

### 🤖 Azure AI Foundry integration

.NET Aspire 9.4 introduces comprehensive Azure AI Foundry support, bringing enterprise AI capabilities directly into your distributed applications. This integration simplifies working with AI models and deployments through the Azure AI platform.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Azure AI Foundry project
var aiFoundry = builder.AddAzureAIFoundry("ai-foundry");

// Add specific model deployments
var gpt4 = aiFoundry.AddDeployment("gpt-4", "gpt-4", "2024-02-15-preview", "OpenAI");
var embedding = aiFoundry.AddDeployment("embedding", "text-embedding-ada-002", "2", "OpenAI");

// Connect your services to AI capabilities
var chatService = builder.AddProject<Projects.ChatService>("chat")
    .WithReference(gpt4)
    .WithReference(embedding);

// For local development, you can run AI Foundry locally
var localAiFoundry = builder.AddAzureAIFoundry("local-ai-foundry")
    .RunAsFoundryLocal(); // Run as local AI Foundry instance

builder.Build().Run();
```

The `RunAsFoundryLocal()` method enables local development scenarios using [Azure AI Foundry Local](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/), allowing you to test AI capabilities without requiring cloud resources during development.

### 🐙 GitHub Models integration

.NET Aspire 9.4 introduces support for GitHub Models, enabling easy integration with AI models hosted on GitHub's platform. This provides a simple way to incorporate AI capabilities into your applications using GitHub's model hosting service.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add GitHub Model with API key
var apiKey = builder.AddParameter("github-api-key", secret: true);
var organization = builder.AddParameter("github-org");

var model = builder.AddGitHubModel("chat-model", "gpt-4o-mini", organization)
    .WithApiKey(apiKey)
    .WithHealthCheck();

// Use the model in your services
var chatService = builder.AddProject<Projects.ChatService>("chat")
    .WithReference(model);

builder.Build().Run();
```

GitHub Models integration provides:
- **Simple model integration** with GitHub's hosted AI models
- **Built-in health checks** for model availability
- **Parameter-based configuration** for API keys and organizations
- **Connection string support** for easy service integration

### 🗄️ Azure Cosmos DB hierarchical partition keys

.NET Aspire 9.4 introduces support for **hierarchical partition keys** in Azure Cosmos DB, enabling more efficient data distribution and querying across multiple partition key paths:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos");
var database = cosmos.AddCosmosDatabase("ecommerce");

// Traditional single partition key
var ordersContainer = database.AddContainer("orders", "/customerId");

// New hierarchical partition keys for better data distribution
var productsContainer = database.AddContainer("products", 
    new[] { "/category", "/subcategory", "/brand" });

// Multi-level partitioning for complex scenarios
var analyticsContainer = database.AddContainer("analytics",
    new[] { "/year", "/month", "/region", "/department" });

builder.Build().Run();
```

Hierarchical partition keys provide:
- **Better data distribution** across multiple logical partitions
- **Improved query performance** for multi-dimensional data
- **Reduced hot partition issues** in large-scale applications
- **More granular scaling** based on access patterns

### ⚡ Serverless support

You can now enable serverless mode for Azure Cosmos DB accounts, perfect for applications with intermittent or unpredictable traffic:

```csharp
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .WithDefaultAzureSku(); // Enables serverless capability
```

This is ideal for development, testing, and production workloads with variable traffic patterns where you want to pay only for the request units and storage you consume.

### 🆔 Enhanced Azure user-assigned managed identity support

.NET Aspire 9.4 introduces comprehensive support for Azure user-assigned managed identities, providing enhanced security and consistent identity management across your Azure infrastructure:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Create a user-assigned managed identity
var appIdentity = builder.AddAzureUserAssignedIdentity("app-identity");

// Create the container app environment
var containerEnv = builder.AddAzureContainerAppEnvironment("container-env");

// Apply the identity to compute resources
var functionApp = builder.AddAzureFunctionsProject<Projects.Functions>("functions")
    .WithAzureUserAssignedIdentity(appIdentity);

// The identity can be shared across multiple resources
var webApp = builder.AddProject<Projects.WebApp>("webapp")
    .WithAzureUserAssignedIdentity(appIdentity);

// Use the same identity for accessing Azure services
var keyVault = builder.AddAzureKeyVault("secrets");
var storage = builder.AddAzureStorage("storage");

// Services using the shared identity can access resources securely
var processor = builder.AddProject<Projects.DataProcessor>("processor")
    .WithAzureUserAssignedIdentity(appIdentity)
    .WithReference(keyVault)
    .WithReference(storage);

builder.Build().Run();
```

This approach provides:
- **Consistent identity management** across all compute resources
- **Enhanced security** without storing connection strings or secrets
- **Simplified access control** using Azure RBAC
- **Better auditability** with centralized identity tracking

### 🔐 Azure Key Vault enhancements

.NET Aspire 9.4 introduces significant improvements to Azure Key Vault integration with new secret management APIs that provide strongly typed access to secrets:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var secrets = builder.AddAzureKeyVault("secrets");

// Add a secret from a parameter
var connectionStringParam = builder.AddParameter("connectionString", secret: true);
var connectionString = secrets.AddSecret("connection-string", connectionStringParam);

// Add a secret with custom secret name in Key Vault
var apiKeyParam = builder.AddParameter("api-key", secret: true);
var apiKey = secrets.AddSecret("api-key", "ApiKey", apiKeyParam);

// Get a secret reference for consumption (for existing secrets)
var existingSecret = secrets.GetSecret("ExistingSecret");

// Use in your services
var webApi = builder.AddProject<Projects.WebAPI>("webapi")
    .WithEnvironment("CONNECTION_STRING", connectionString)
    .WithEnvironment("API_KEY", apiKey)
    .WithEnvironment("EXISTING_SECRET", existingSecret);
```

**Key features**:
- **`AddSecret()`** method for adding new secrets to Key Vault from parameters or expressions
- **`GetSecret()`** method for referencing existing secrets in Key Vault
- **Strongly-typed secret references** that can be used with `WithEnvironment()` for environment variables
- **Custom secret naming** support with optional `secretName` parameter

### 📊 Azure Storage client improvements

.NET Aspire 9.4 standardizes Azure Storage client registration with consistent naming conventions and enhanced keyed service support:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Azure Table Storage with new standardized naming
builder.AddAzureTableServiceClient("tables");
builder.AddKeyedAzureTableServiceClient("primary-tables");

// Azure Queue Storage with consistent API
builder.AddAzureQueueServiceClient("queues");
builder.AddKeyedAzureQueueServiceClient("primary-queues");

// Keyed Blob Storage (enhanced support)
builder.AddKeyedAzureBlobServiceClient("primary-blobs");
```

The new client registration methods provide consistent naming across all Azure Storage services and full support for keyed dependency injection scenarios.

### 🔧 Enhanced tracing configuration

Azure components now support granular tracing control with new `DisableTracing` options:

```csharp
// Azure App Configuration with tracing disabled
builder.AddAzureAppConfiguration("config", settings =>
{
    settings.DisableTracing = true; // Disable OpenTelemetry tracing
});

// Other Azure services also support this configuration
builder.AddAzureKeyVault("vault", settings =>
{
    settings.DisableTracing = true;
});

builder.AddAzureServiceBus("servicebus", settings =>
{
    settings.DisableTracing = true;
});
```

This is particularly useful for:
- **Performance optimization** in high-throughput scenarios
- **Reducing telemetry noise** for auxiliary services
- **Selective tracing** in complex applications
- **Compliance scenarios** where certain traces should not be collected

### 🐳 Docker Compose with integrated Aspire Dashboard

Managing observability in Docker Compose environments often requires running separate monitoring tools or losing the rich insights that Aspire provides during development. .NET Aspire 9.4 introduces native Aspire Dashboard integration for Docker Compose environments.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("production")
                    .WithDashboard(enabled: true);

// Add services that will automatically report to the dashboard
builder.AddProject<Projects.Frontend>("frontend")
       .WithComputeEnvironment(compose);

builder.AddProject<Projects.Api>("api")
       .WithComputeEnvironment(compose);

builder.Build().Run();
```

The dashboard automatically configures OTLP endpoints and service discovery, giving you the same rich observability experience in production Docker environments that you have during local development.

### 🔧 Resource lifecycle events

Understanding when resources become available or encounter issues during startup has been challenging, especially in complex distributed applications. .NET Aspire 9.4 introduces a comprehensive eventing system that lets you hook into key moments in the resource lifecycle.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres");

var api = builder.AddProject<Projects.Api>("api")
                .WithReference(database)
                .OnConnectionStringAvailable(async (resource, evt, cancellationToken) =>
                {
                    // Log when connection strings are resolved
                    var logger = evt.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Connection string available for {Name}", resource.Name);
                })
                .OnBeforeResourceStarted(async (resource, evt, cancellationToken) =>
                {
                    // Pre-startup validation or configuration
                    var serviceProvider = evt.Services;
                    // Additional validation logic here
                })
                .OnResourceReady(async (resource, evt, cancellationToken) =>
                {
                    // Resource is fully ready
                    var logger = evt.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Resource {Name} is ready", resource.Name);
                });

builder.Build().Run();
```

These events enable sophisticated startup orchestration, health validation, and initialization workflows without requiring complex external coordination mechanisms.

### 🌐 External service modeling

Modern applications frequently need to integrate with external APIs, third-party services, or existing infrastructure that isn't managed by Aspire. .NET Aspire 9.4 introduces first-class support for modeling external services as resources in your application graph.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Reference an external service by URL
var externalApi = builder.AddExternalService("external-api", "https://api.company.com");

// Or use a parameter for dynamic configuration
var apiUrl = builder.AddParameter("api-url");
var externalDb = builder.AddExternalService("external-db", apiUrl)
    .WithHttpHealthCheck("/health");

var myService = builder.AddProject<Projects.MyService>("my-service")
    .WithReference(externalApi)
    .WithReference(externalDb);

builder.Build().Run();
```

External services appear in the Aspire dashboard with health status, can be referenced like any other resource, and support the same configuration patterns as internal resources.

### ✨ Enhanced emulator support

.NET Aspire 9.4 introduces better support for **emulator resources** with improved detection and management:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add resources that might be emulators
var storage = builder.AddAzureStorage("storage");
var cosmos = builder.AddAzureCosmosDB("cosmos");
var redis = builder.AddRedis("cache");

// Check if resources are running as emulators
builder.Services.AddHostedService<EmulatorDetectionService>();

public class EmulatorDetectionService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Use the new IsEmulator extension method
        if (storage.Resource.IsEmulator())
        {
            Console.WriteLine("Storage is running in emulator mode");
            // Configure for local development
        }
        
        if (cosmos.Resource.IsEmulator())
        {
            Console.WriteLine("Cosmos DB is running in emulator mode");
            // Skip certain production-only features
        }
    }
}

// You can also configure emulator-specific behavior
var app = builder.AddProject<Projects.MyApp>("app")
    .WithReference(storage)
    .WithEnvironment("STORAGE_EMULATOR", storage.Resource.IsEmulator() ? "true" : "false");

builder.Build().Run();
```

### 📁 Enhanced container file mounting

Configuring container file systems often requires understanding complex Docker volume syntax and managing file permissions manually. .NET Aspire 9.4 introduces enhanced file mounting APIs that handle common scenarios with sensible defaults.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Simple file copying from local source to container
var myContainer = builder.AddContainer("myapp", "myapp:latest")
    .WithContainerFiles("/app/config", "./config-files")
    .WithContainerFiles("/app/data", "./data", defaultOwner: 1000, defaultGroup: 1000)
    .WithContainerFiles("/app/scripts", "./scripts", umask: UnixFileMode.UserRead | UnixFileMode.UserWrite);

// You can also use the callback approach for dynamic file generation
var dynamicContainer = builder.AddContainer("worker", "worker:latest")
    .WithContainerFiles("/app/runtime-config", async (context, ct) =>
    {
        // Generate configuration files dynamically
        var configFile = new ContainerFileSystemItem
        {
            Name = "app.json",
            Contents = JsonSerializer.SerializeToUtf8Bytes(new { Environment = "Production" })
        };
        
        return new[] { configFile };
    });

builder.Build().Run();
```

The enhanced APIs handle file permissions, ownership, and provide both static and dynamic file mounting capabilities while maintaining the flexibility to customize when needed.

### 🎭 Experimental interaction services

.NET Aspire 9.4 introduces experimental support for rich user interactions during resource operations. This enables more sophisticated automation and deployment workflows through a standardized interaction system that works across multiple scenarios including dashboard run mode and CLI deploy/publish operations.

> [!IMPORTANT]
> 🧪 This feature is experimental and may change in future releases.

The interaction system supports:

- Confirmation prompts for destructive operations
- Input collection with validation
- Multi-step workflows
- Integration with both console and IDE environments
- Dashboard interactions during run mode
- CLI interactions during deploy and publish operations

```csharp
// Example usage of IInteractionService APIs
public class DeploymentService
{
    private readonly IInteractionService _interactionService;
    
    public DeploymentService(IInteractionService interactionService)
    {
        _interactionService = interactionService;
    }
    
    public async Task DeployAsync()
    {
        // Prompt for confirmation before destructive operations
        var confirmResult = await _interactionService.PromptConfirmationAsync(
            "Confirm Deployment", 
            "This will overwrite the existing deployment. Continue?");
            
        if (!confirmResult.IsOk || !confirmResult.Data)
            return;
        
        // Collect deployment parameters
        var inputResult = await _interactionService.PromptInputAsync(
            "Deployment Configuration",
            "Enter the target environment:",
            "Environment",
            "production");
            
        if (inputResult.IsOk)
        {
            var environment = inputResult.Data.Value;
            // Proceed with deployment using the collected input
        }
        
        // Collect multiple inputs with validation
        var inputs = new List<InteractionInput>
        {
            new() { Label = "Region", InputType = InputType.Text, Required = true },
            new() { Label = "Instance Count", InputType = InputType.Number, Required = true },
            new() { Label = "Enable Monitoring", InputType = InputType.Boolean, Required = false }
        };
        
        var multiInputResult = await _interactionService.PromptInputsAsync(
            "Advanced Configuration",
            "Configure deployment settings:",
            inputs,
            new InputsDialogInteractionOptions
            {
                ValidationCallback = async context =>
                {
                    var regionInput = context.Inputs.First(i => i.Label == "Region");
                    if (!IsValidRegion(regionInput.Value))
                    {
                        context.AddValidationError(regionInput, "Invalid region specified");
                    }
                }
            });
        
        // Show progress notifications
        await _interactionService.PromptNotificationAsync(
            "Deployment Status",
            "Deployment completed successfully!",
            new NotificationInteractionOptions
            {
                Intent = MessageIntent.Success,
                LinkText = "View Dashboard",
                LinkUrl = "https://portal.azure.com"
            });
    }
    
    private bool IsValidRegion(string? region) 
    {
        // Validation logic here
        return !string.IsNullOrEmpty(region);
    }
}
```

The `IInteractionService` interface provides several methods for user interaction:

- `PromptConfirmationAsync()` - Yes/No confirmation dialogs
- `PromptInputAsync()` - Single input collection
- `PromptInputsAsync()` - Multiple input collection with validation
- `PromptMessageBoxAsync()` - Informational message displays
- `PromptNotificationAsync()` - Status notifications

**Input types supported:**
- `Text` - Standard text input
- `SecretText` - Password/secret input (masked)
- `Choice` - Dropdown selection
- `Boolean` - Checkbox input
- `Number` - Numeric input

**Advanced features:**
- **Validation callbacks** for complex input validation
- **Markdown support** for rich text descriptions
- **Custom button text** and dialog options
- **Intent-based styling** for different message types
- **Link support** in notifications

These interactions work seamlessly whether you're running your application through the Aspire dashboard or deploying via the CLI with `aspire deploy` and `aspire publish` commands.

## ☁️ Azure goodies

### 🔄 Flexible Azure Storage queue management

Working with Azure Storage queues often requires choosing between coarse-grained connection to entire storage accounts or manually managing individual queue configurations. .NET Aspire 9.4 introduces fine-grained queue modeling that lets you work with specific queues while maintaining clear separation of concerns.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage");

// Model individual queues as first-class resources
var orderQueue = storage.AddQueue("orders", "order-processing");
var notificationQueue = storage.AddQueue("notifications", "user-notifications");

// Services get scoped access to specific queues
builder.AddProject<Projects.OrderProcessor>("order-processor")
       .WithReference(orderQueue);  // Only has access to order-processing queue

builder.AddProject<Projects.NotificationService>("notifications")
       .WithReference(notificationQueue);  // Only has access to user-notifications queue

builder.Build().Run();
```

This approach provides better security isolation, clearer dependency modeling, and simplifies service configuration by injecting pre-configured `QueueClient` instances.

### 🏗️ Enhanced Azure Container Apps integration

Managing complex Azure Container Apps environments often requires integrating with existing Azure resources like Log Analytics workspaces. .NET Aspire 9.4 enhances Container Apps integration with support for existing Azure resources and improved configuration.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Reference existing Log Analytics workspace
var workspaceName = builder.AddParameter("workspace-name");
var workspaceRg = builder.AddParameter("workspace-rg");

var logWorkspace = builder.AddAzureLogAnalyticsWorkspace("workspace")
                          .AsExisting(workspaceName, workspaceRg);

var containerEnv = builder.AddAzureContainerAppEnvironment("production")
                          .WithAzureLogAnalyticsWorkspace(logWorkspace);

builder.AddProject<Projects.Api>("api")
       .WithComputeEnvironment(containerEnv);

builder.Build().Run();
```

The enhanced integration provides better cost control by reusing existing Log Analytics workspaces and improved resource coordination.

### 🔗 Improved Azure resource naming

Consistent resource naming across environments and deployments is crucial for Azure resource management, but has often required custom naming logic or post-deployment coordination. .NET Aspire 9.4 introduces improved output reference support that makes it easier to reference and coordinate Azure resources.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("appstorage");
var signalr = builder.AddAzureSignalR("notifications");

// The NameOutputReference provides consistent, deployment-time resource names
var api = builder.AddProject<Projects.Api>("api")
                .WithEnvironment("STORAGE_NAME", storage.Resource.NameOutputReference)
                .WithEnvironment("SIGNALR_NAME", signalr.Resource.NameOutputReference);

builder.Build().Run();
```

This ensures your applications can reliably reference Azure resources by their actual deployed names, improving coordination between services and external automation.

The `NameOutputReference` property is now available on all Azure resources including:
- Azure App Configuration
- Azure App Containers Environment
- Azure Application Insights
- Azure Cosmos DB
- Azure Event Hubs
- Azure Key Vault
- Azure PostgreSQL
- Azure Redis Cache
- Azure AI Search
- Azure Service Bus
- Azure SignalR
- Azure SQL Database
- Azure Storage
- Azure Web PubSub
- And many more Azure services

### 🎯 Enhanced emulator consistency

Azure emulator resources now include `EmulatorResourceAnnotation` for consistent tooling support across all emulator implementations, providing better development experience and tooling integration.

## 🔧 Integrations updates

### ✨ Database hosting improvements

Several database integrations have been updated with **improved initialization patterns**:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// MongoDB - new WithInitFiles method (replaces WithInitBindMount)
var mongo = builder.AddMongoDB("mongo")
    .WithInitFiles("./mongo-init");  // Initialize with scripts

// MySQL - improved initialization with better file handling
var mysql = builder.AddMySql("mysql", password: builder.AddParameter("mysql-password"))
    .WithInitFiles("./mysql-init");  // Initialize with SQL scripts

// Oracle - enhanced setup capabilities with consistent API
var oracle = builder.AddOracle("oracle")
    .WithInitFiles("./oracle-init");  // Initialize with Oracle scripts

builder.Build().Run();
```

**Key improvements**:
- **Unified initialization**: All database providers now support `WithInitFiles()` method
- **Simplified API**: Replaces the more complex `WithInitBindMount()` method
- **Better error handling**: The new method provides improved error handling

### ✨ Keycloak realm import simplification

Keycloak integration has received a **breaking change** that simplifies the `WithRealmImport` method:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// ❌ Before (9.3 and earlier):
var keycloak = builder.AddKeycloak("keycloak")
    .WithRealmImport("./realm.json", isReadOnly: false);  // Required parameter

// ✅ After (9.4):
var keycloak = builder.AddKeycloak("keycloak")
    .WithRealmImport("./realm.json");  // Simplified - parameter removed

// The method now has two overloads:
// 1. WithRealmImport(string import) - uses default behavior
// 2. WithRealmImport(string import, bool isReadOnly) - explicit control when needed

builder.Build().Run();
```

**What changed**: The `isReadOnly` parameter was removed as a default parameter and is now only available when explicitly needed, making the common case simpler while still allowing advanced scenarios.

## 🖥️ CLI enhancements

The Aspire CLI is now **generally available** with significant improvements and new capabilities.

### 🚀 New AOT-compiled CLI for superior performance

The biggest improvement in Aspire 9.4 is the introduction of **AOT-compiled (Ahead-of-Time) versions** of the CLI that deliver dramatically better performance compared to traditional .NET Global Tools.

**Why AOT matters for the CLI**:

## 🖥️ CLI enhancements

The Aspire CLI is now **generally available** with significant improvements and new capabilities.

### 🚀 New AOT-compiled CLI for superior performance

The biggest improvement in Aspire 9.4 is the introduction of **AOT-compiled (Ahead-of-Time) versions** of the CLI that deliver dramatically better performance compared to traditional .NET Global Tools.

**Why AOT matters for the CLI**:

## 🖥️ CLI enhancements

The Aspire CLI is now **generally available** with significant improvements and new capabilities.

### � New AOT-compiled CLI for superior performance

The biggest improvement in Aspire 9.4 is the introduction of **AOT-compiled (Ahead-of-Time) versions** of the CLI that deliver dramatically better performance compared to traditional .NET Global Tools.

**Why AOT matters for the CLI**:
- ⚡ **Instant startup**: No JIT compilation overhead - commands start executing immediately
- 🏃‍♂️ **Faster execution**: 2-3x faster command execution for common operations like `aspire run` and `aspire new`
- 💾 **Lower memory usage**: Reduced memory footprint with optimized native binaries
- 📦 **Self-contained**: No dependency on .NET runtime installation - works out of the box
- 🔧 **Better tooling integration**: Faster response times when used in CI/CD pipelines and scripts

📦 **Recommended installation** (AOT-compiled for maximum performance):

```bash
# Linux/macOS
curl -sSL https://aspire.dev/install.sh | bash

# Windows (PowerShell)
iex "& { $(irm https://aspire.dev/install.ps1) }"
```

**Performance benefits you'll notice immediately**:
- `aspire run` starts your applications **2-3x faster**
- `aspire new` project creation is **significantly more responsive**
- `aspire add` package operations complete **much quicker**
- All CLI commands have **near-zero startup latency**

📦 **Alternative installation** (via .NET Global Tool - slower but compatible):

```bash
dotnet tool install --global aspire.cli
```

> [!TIP]
> **Recommendation**: Use the AOT installation scripts above for the best experience. The Global Tool version is provided for compatibility scenarios where the AOT version cannot be used.

> [!NOTE]
> ⚠️ **The Aspire 9.4 CLI is not compatible with Aspire 9.3 projects.**
> You must upgrade your project to Aspire 9.4+ in order to use the latest CLI features.

### ✨ New `aspire exec` command

A new **exec command** allows you to execute commands within the context of your Aspire application environment:

```bash
# Execute commands with environment variables from your app model
aspire exec --resource my-api -- dotnet ef database update

# Run scripts with access to application context
aspire exec --start-resource my-worker -- npm run build

# The exec command automatically provides environment variables
# from your Aspire application resources to the executed command
```

**Key capabilities**:
- **Environment variable injection** from your app model resources
- **Resource targeting** with `--resource` or `--start-resource` options
- **Command execution** in the context of your Aspire application

> [!IMPORTANT]
> 🧪 **Feature Flag**: The `aspire exec` command is behind a feature flag and **disabled by default** in this release. It must be explicitly enabled for use.

### 🚀 Enhanced `aspire deploy` command

The `aspire deploy` command has been significantly enhanced with better user experience and infrastructure:

```bash
# Deploy your Aspire application (when feature flag is enabled)
aspire deploy --help
```

> [!IMPORTANT]
> 🧪 **Feature Flag**: The `aspire deploy` command is also behind a feature flag and available as a preview feature in this release.

**Key improvements**:
- **Context-sensitive completion messages** for better user guidance
- **Clean infrastructure** with improved publish/deploy command architecture
- **Enhanced user experience** with better prompting and feedback

### 🚩 Feature Flag Configuration

Both the `exec` and `deploy` commands are controlled by feature flags that must be explicitly enabled:

```bash
# Enable exec command functionality
aspire config set features.execCommandEnabled true

# Enable deploy command functionality  
aspire config set features.deployCommandEnabled true

# View current configuration
aspire config list

# Check if a feature is enabled
aspire config get features.execCommandEnabled
aspire config get features.deployCommandEnabled
```

**Important notes**:
- Feature flags are **disabled by default** for both commands
- When disabled, the commands will not appear in CLI help or be available for use
- Configuration uses the `features.` prefix for all feature flag settings
- This allows for controlled rollout and testing of new functionality

### 🎯 Enhanced resource interaction capabilities

The Aspire CLI in 9.4 introduces experimental support for rich user interactions, enabling more sophisticated automation and deployment workflows. This includes support for confirmation prompts, input collection, and validation workflows during resource operations.

These capabilities are particularly useful for:

- Collecting deployment parameters interactively
- Confirming destructive operations
- Gathering configuration input during setup workflows

The interaction system integrates with both console-based workflows and can be extended to work with IDE integrations and automated tooling.

### ⚙️ New configuration management commands

The CLI now includes comprehensive configuration management capabilities:

```bash
# Set configuration values with dot notation support
aspire config set dashboard.theme dark
aspire config set telemetry.enabled false

# Get configuration values
aspire config get dashboard.theme
aspire config list

# Delete configuration values
aspire config delete dashboard.theme
aspire config delete telemetry.enabled
```

**Key features**:
- **Dot notation support** for hierarchical configuration keys
- **User-specific settings** stored in `.aspire` directory
- **Consolidated config commands** with intuitive verb-based syntax
- **Automatic fallback** when app host files in settings don't exist

### 🎨 Enhanced CLI user experience

Several CLI experience improvements have been added in 9.4:

- **Purple styling** for default values in CLI prompts for better visual distinction
- **Markup escaping** fixes for better rendering in various terminal environments
- **User-friendly error handling** for `aspire new` when directories contain existing files
- **Enhanced template selection** with pre-release package support
- **Localization support** for better international user experience
- **Health column** added to `aspire run` resources table for better status visibility
- **Version update notifications** to alert users when new CLI versions are available
- **Improved grid display** with right-aligned labels and padding in resource tables
- **Better package filtering** and duplicate package handling in `aspire add`
- **Enhanced completion messages** with context-sensitive feedback
- **.NET SDK availability checks** to ensure proper environment setup

```bash
# Improved new project experience with better error handling
aspire new

# Enhanced package addition with better prompts and styling
aspire add

# Enhanced resource display with health status and dashboard integration
aspire run

# Configuration management with dot notation support
aspire config set key.subkey value
```

**Key `aspire run` enhancements**:
- **Dashboard URL display**: Shows the local dashboard URL for easy access
- **Log link integration**: Provides direct links to application logs
- **Health status column**: Real-time health monitoring of all resources
- **Improved resource table**: Better formatting and status indicators

### 🔧 CLI infrastructure improvements

Behind the scenes, the CLI has received significant infrastructure enhancements, with **AOT compilation** being the standout improvement:

**🚀 AOT Compilation Benefits**:
- **Native performance**: AOT-compiled binaries run at native speeds without JIT overhead
- **Instant startup**: Commands execute immediately - no warm-up time required
- **Reduced resource usage**: Lower CPU and memory consumption during operation
- **Better CI/CD performance**: Faster build pipelines and deployment scripts
- **Improved developer experience**: More responsive tooling that doesn't interrupt your flow

**Additional infrastructure improvements**:
- **MSBuild Terminal Logger** is now automatically disabled for all `dotnet` command executions
- **Improved error handling** when dashboard fails to start with proper exception handling
- **Enhanced app host search** with better messaging and user experience
- **Shared activity source** across CLI components for better observability
- **Thread safety improvements** in app host project file discovery
- **Graceful offline scenario handling** for `aspire new` and `aspire add` commands

> [!NOTE]
> The performance improvements from AOT compilation are most noticeable on the first command execution, where traditional .NET tools would spend time on JIT compilation. With AOT, every command runs at full speed from the very first use.

## 💔 Breaking changes

### Azure Storage API consolidation

.NET Aspire 9.4 consolidates Azure Storage APIs for better consistency and security isolation. Several methods on specialized storage resource builders have been marked as obsolete in favor of unified methods on the main `AzureStorageResource`.

#### Blob Storage API changes

```csharp
// ❌ Before (obsolete):
var storage = builder.AddAzureStorage("storage");
var blobs = storage.AddBlobs("blobs");
var container = blobs.AddBlobContainer("images");     // Obsolete
var blobService = blobs.AddBlobService("service");    // Obsolete

// ✅ After (recommended):
var storage = builder.AddAzureStorage("storage");
var container = storage.AddBlobContainer("images");   // Direct on storage
var blobService = storage.AddBlobService("service");  // Direct on storage
```

#### Queue Storage API changes

```csharp
// ❌ Before (obsolete):
var storage = builder.AddAzureStorage("storage");
var queues = storage.AddQueues("queues");
var queueService = queues.AddQueueService("service"); // Obsolete

// ✅ After (recommended):
var storage = builder.AddAzureStorage("storage");
var queueService = storage.AddQueueService("service"); // Direct on storage
```

#### Table Storage API changes

```csharp
// ❌ Before (obsolete):
var storage = builder.AddAzureStorage("storage");
var tables = storage.AddTables("tables");
var tableService = tables.AddTableService("service"); // Obsolete

// ✅ After (recommended):
var storage = builder.AddAzureStorage("storage");
var tableService = storage.AddTableService("service"); // Direct on storage
```

**Migration impact**: Update all Azure Storage service references to use the consolidated APIs directly on `AzureStorageResource` instead of the specialized resource builders.

### Azure Storage component registration updates

Client registration methods for Azure Storage have been standardized with new naming conventions:

```csharp
// ❌ Before (obsolete):
builder.Services.AddAzureTableClient("tables");         // Obsolete
builder.Services.AddKeyedAzureTableClient("tables");    // Obsolete
builder.Services.AddAzureBlobClient("blobs");            // Obsolete
builder.Services.AddKeyedAzureBlobClient("blobs");       // Obsolete
builder.Services.AddAzureQueueClient("queues");          // Obsolete
builder.Services.AddKeyedAzureQueueClient("queues");     // Obsolete

// ✅ After (recommended):
builder.Services.AddAzureTableServiceClient("tables");         // Standardized naming
builder.Services.AddKeyedAzureTableServiceClient("tables");    // Standardized naming
builder.Services.AddAzureBlobServiceClient("blobs");           // Standardized naming
builder.Services.AddKeyedAzureBlobServiceClient("blobs");      // Standardized naming
builder.Services.AddAzureQueueServiceClient("queues");         // Standardized naming
builder.Services.AddKeyedAzureQueueServiceClient("queues");    // Standardized naming
```

**Migration impact**: Update all client registration calls to use the new `*ServiceClient` naming convention.

### Azure Key Vault secret reference changes

Azure Key Vault secret handling has been updated with improved APIs that provide better type safety and consistency:

```csharp
// ❌ Before (obsolete):
var keyVault = builder.AddAzureKeyVault("secrets");
var secretOutput = keyVault.GetSecretOutput("ApiKey");           // Obsolete
var secretRef = new BicepSecretOutputReference(secretOutput);    // Obsolete - class removed

// ✅ After (recommended):
var keyVault = builder.AddAzureKeyVault("secrets");
var secretRef = keyVault.GetSecret("ApiKey");                    // New strongly-typed API

// For environment variables:
// ❌ Before (obsolete):
builder.AddProject<Projects.Api>("api")
       .WithEnvironment("API_KEY", secretRef);  // Using BicepSecretOutputReference

// ✅ After (recommended):
builder.AddProject<Projects.Api>("api")
       .WithEnvironment("API_KEY", secretRef);  // Using IAzureKeyVaultSecretReference
```

**Migration impact**: Replace `GetSecretOutput()` and `BicepSecretOutputReference` usage with the new `GetSecret()` method that returns `IAzureKeyVaultSecretReference`.

### Database initialization method changes

Several database resources have **deprecated `WithInitBindMount` in favor of the more consistent `WithInitFiles`**:

```csharp
// ❌ Before (deprecated):
var mongo = builder.AddMongoDB("mongo")
    .WithInitBindMount("./init", isReadOnly: true);  // Complex parameters

var mysql = builder.AddMySql("mysql")  
    .WithInitBindMount("./mysql-scripts", isReadOnly: false);

var oracle = builder.AddOracle("oracle")
    .WithInitBindMount("./oracle-init", isReadOnly: true);

var postgres = builder.AddPostgres("postgres")
    .WithInitBindMount("./postgres-init", isReadOnly: true);

// ✅ After (recommended):
var mongo = builder.AddMongoDB("mongo")
    .WithInitFiles("./init");  // Simplified, consistent API

var mysql = builder.AddMySql("mysql")
    .WithInitFiles("./mysql-scripts");  // Same pattern across all providers

var oracle = builder.AddOracle("oracle")
    .WithInitFiles("./oracle-init");  // Unified approach

var postgres = builder.AddPostgres("postgres")
    .WithInitFiles("./postgres-init");  // Consistent across all databases
```

**Affected database providers**: MongoDB, MySQL, Oracle, and PostgreSQL

**Migration impact**: Replace `WithInitBindMount()` calls with `WithInitFiles()` - the new method handles read-only mounting automatically and provides better error handling.

### Keycloak realm import simplification

The `WithRealmImport` method signature has been **simplified by removing the confusing `isReadOnly` parameter**:

```csharp
// ❌ Before (deprecated):
var keycloak = builder.AddKeycloak("keycloak")
    .WithRealmImport("./realm.json", isReadOnly: false);  // Confusing parameter

// ✅ After (recommended):
var keycloak = builder.AddKeycloak("keycloak")
    .WithRealmImport("./realm.json");  // Clean, simple API

// If you need explicit read-only control:
var keycloak = builder.AddKeycloak("keycloak")
    .WithRealmImport("./realm.json", isReadOnly: true);  // Still available as overload
```

**Migration impact**: Remove the `isReadOnly` parameter from single-parameter `WithRealmImport()` calls - the method now defaults to appropriate behavior. Use the two-parameter overload if explicit control is needed.

### Resource lifecycle event updates

The generic `AfterEndpointsAllocatedEvent` has been deprecated in favor of more specific, type-safe events:

```csharp
// ❌ Before (deprecated):
builder.Services.AddSingleton<IDistributedApplicationLifecycleHook, MyLifecycleHook>();

public class MyLifecycleHook : IDistributedApplicationLifecycleHook
{
    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        // Generic event handling - deprecated
        return Task.CompletedTask;
    }
}

// ✅ After (recommended):
var api = builder.AddProject<Projects.Api>("api")
    .OnBeforeResourceStarted(async (resource, evt, cancellationToken) =>
    {
        // Resource-specific event handling
    })
    .OnResourceEndpointsAllocated(async (resource, evt, cancellationToken) =>
    {
        // Endpoint-specific event handling
    });
```

**Migration impact**: Replace usage of `AfterEndpointsAllocatedEvent` with resource-specific lifecycle events like `OnBeforeResourceStarted` or `OnResourceEndpointsAllocated` for better type safety and clarity.

### Known parameter deprecations

Several well-known parameters have been deprecated in favor of resource-based approaches:

```csharp
// ❌ Before (deprecated):
var keyVaultName = builder.AddParameter("keyvault-name");
builder.AddAzureKeyVault("secrets", keyVaultName);  // Using KnownParameters.KeyVaultName

var workspaceId = builder.AddParameter("workspace-id");
// Using KnownParameters.LogAnalyticsWorkspaceId

// ✅ After (recommended):
var keyVault = builder.AddAzureKeyVault("secrets");  // Direct resource modeling
var workspace = builder.AddAzureLogAnalyticsWorkspace("workspace");  // Direct resource modeling
```

**Migration impact**: Replace parameter-based resource references with direct resource modeling using the `Add*` methods for better type safety and IntelliSense support.

### Kafka configuration method changes

Kafka configuration has been updated with more descriptive method names:

```csharp
// ❌ Before (deprecated):
var kafka = builder.AddKafka("kafka")
    .WithConfigurationFile("./kafka.properties");  // Old method name

// ✅ After (recommended):
var kafka = builder.AddKafka("kafka")
    .WithConfigurationFile("./kafka.properties");  // Method renamed for clarity
```

**Migration impact**: Update method calls to use the new naming convention if you were using preview Kafka configuration methods.

With every release, we strive to make .NET Aspire better. However, some changes may break existing functionality. For complete details on breaking changes in this release, see:

- [Breaking changes in .NET Aspire 9.4](../compatibility/9.4/index.md)

## 🎯 Upgrade today

Follow the directions outlined in the [Upgrade to .NET Aspire 9.4](#-upgrade-to-net-aspire-94) section to make the switch to 9.4 and take advantage of all these new features today! As always, we're listening for your feedback on [GitHub](https://github.com/dotnet/aspire/issues)—and looking out for what you want to see in 9.5 ☺️.

For a complete list of issues addressed in this release, see [.NET Aspire GitHub repository—9.4 milestone](https://github.com/dotnet/aspire/issues?q=is%3Aissue%20state%3Aclosed%20milestone%3A9.4%20).
