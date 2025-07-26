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

var yarp = builder.AddYarp("gateway");

// Configure sophisticated routing with transforms
yarp.AddRoute("api-v1", "/api/v1/{**catch-all}")
    .To(backendApi)
    .WithTransformPathPrefix("/v2")  // Rewrite /api/v1/* to /v2/*
    .WithTransformRequestHeader("X-API-Version", "2.0")
    .WithTransformForwarded(useHost: true, useProto: true)
    .WithTransformResponseHeader("X-Powered-By", "Aspire Gateway");

// Advanced header and query manipulation
yarp.AddRoute("auth", "/auth/{**catch-all}")
    .To(identityService)
    .WithTransformClientCertHeader("X-Client-Cert")
    .WithTransformQueryValue("client_id", "aspire-app")
    .WithTransformRequestHeadersAllowed("Authorization", "Content-Type")
    .WithTransformUseOriginalHostHeader(false);

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
var gpt4 = aiFoundry.AddDeployment("gpt-4", "gpt-4", "2024-02-15-preview");
var embedding = aiFoundry.AddDeployment("embedding", "text-embedding-ada-002", "2");

// Connect your services to AI capabilities
var chatService = builder.AddProject<Projects.ChatService>("chat")
    .WithReference(gpt4)
    .WithReference(embedding);

// For local development, you can run AI Foundry locally
var localAiFoundry = builder.AddAzureAIFoundry("local-ai-foundry")
    .RunAsFoundryLocal(); // Run as local AI Foundry instance
```

The `RunAsFoundryLocal()` method enables local development scenarios using [Azure AI Foundry Local](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/), allowing you to test AI capabilities without requiring cloud resources during development.

### 🔐 Azure Key Vault enhancements

.NET Aspire 9.4 introduces significant improvements to Azure Key Vault integration with new secret management APIs that provide strongly typed access to secrets:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var secrets = builder.AddAzureKeyVault("secrets");

// Add a secret reference
var connectionString = secrets.AddSecret("ConnectionString");

// Get a secret for consumption
var apiKey = secrets.GetSecret("ApiKey");

// Use in your services
var webApi = builder.AddProject<Projects.WebAPI>("webapi")
    .WithReference(connectionString)
    .WithEnvironment("API_KEY", apiKey);
```

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

### 🔧 Azure resource infrastructure improvements

.NET Aspire 9.4 introduces enhanced Azure resource provisioning capabilities with the `AddAsExistingResource` pattern and new infrastructure annotations:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Use existing Azure resources
var existingStorage = builder.AddAzureStorage("storage")
    .AddAsExistingResource(); // Reference existing Azure Storage account

// Enhanced emulator support with annotations
var cosmosDb = builder.AddAzureCosmosDB("cosmos")
    .WithEmulatorResourceAnnotation() // Mark as emulator-compatible
    .WithAzureLogAnalyticsWorkspaceReference(); // Link to Log Analytics
```

### 🔧 Enhanced tracing configuration

Azure components now support granular tracing control with new `DisableTracing` options:

```csharp
// Azure App Configuration with tracing disabled
builder.AddAzureAppConfiguration("config", settings =>
{
    settings.DisableTracing = true; // Disable OpenTelemetry tracing
});
```

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
        
        // Show progress notifications
        await _interactionService.PromptNotificationAsync(
            "Deployment Status",
            "Deployment completed successfully!");
    }
}
```

The `IInteractionService` interface provides several methods for user interaction:

- `PromptConfirmationAsync()` - Yes/No confirmation dialogs
- `PromptInputAsync()` - Single input collection
- `PromptInputsAsync()` - Multiple input collection with validation
- `PromptMessageBoxAsync()` - Informational message displays
- `PromptNotificationAsync()` - Status notifications

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

🧪 The Aspire CLI is **still in preview** and under active development. Expect more features and polish in future releases.

📦 To install:

```bash
dotnet tool install --global aspire.cli --prerelease
```

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

**Note**: This command is currently behind a feature flag and marked as preview functionality.

### 🎯 Enhanced resource interaction capabilities

The Aspire CLI in 9.4 introduces experimental support for rich user interactions, enabling more sophisticated automation and deployment workflows. This includes support for confirmation prompts, input collection, and validation workflows during resource operations.

These capabilities are particularly useful for:

- Collecting deployment parameters interactively
- Confirming destructive operations
- Gathering configuration input during setup workflows

The interaction system integrates with both console-based workflows and can be extended to work with IDE integrations and automated tooling.

## 💔 Breaking changes

### Azure Storage API updates

The `AddBlobContainer` method on `IResourceBuilder<AzureBlobStorageResource>` has been marked as obsolete. Use the new `AddBlobContainer` method on `IResourceBuilder<AzureStorageResource>` instead, which provides better resource modeling and security isolation.

**Before:**
```csharp
var blobs = storage.AddBlobs("blobs");
var container = blobs.AddBlobContainer("images"); // Obsolete
```

**After:**
```csharp
var container = storage.AddBlobContainer("images"); // Preferred approach
```

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

// ✅ After (recommended):
var mongo = builder.AddMongoDB("mongo")
    .WithInitFiles("./init");  // Simplified, consistent API

var mysql = builder.AddMySql("mysql")
    .WithInitFiles("./mysql-scripts");  // Same pattern across all providers

var oracle = builder.AddOracle("oracle")
    .WithInitFiles("./oracle-init");  // Unified approach
```

**Affected database providers**: MongoDB, MySQL, and Oracle

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
```

**Migration impact**: Remove the `isReadOnly` parameter from `WithRealmImport()` calls - the method now automatically handles realm import configuration.

### Docker Compose configuration changes

The Docker Compose configuration model has been enhanced to support richer scenarios. If you were using custom Docker Compose configuration callbacks, review the new strongly-typed configuration APIs for better type safety and IntelliSense support.

With every release, we strive to make .NET Aspire better. However, some changes may break existing functionality. For complete details on breaking changes in this release, see:

- [Breaking changes in .NET Aspire 9.4](../compatibility/9.4/index.md)

## 🎯 Upgrade today

Follow the directions outlined in the [Upgrade to .NET Aspire 9.4](#-upgrade-to-net-aspire-94) section to make the switch to 9.4 and take advantage of all these new features today! As always, we're listening for your feedback on [GitHub](https://github.com/dotnet/aspire/issues)—and looking out for what you want to see in 9.5 ☺️.

For a complete list of issues addressed in this release, see [.NET Aspire GitHub repository—9.4 milestone](https://github.com/dotnet/aspire/issues?q=is%3Aissue%20state%3Aclosed%20milestone%3A9.4%20).
