---
title: What's new in .NET Aspire 9.4
description: Learn what's new in .NET Aspire 9.4.
ms.date: 07/26/2025
---

# What's new in .NET Aspire 9.4

📢 .NET Aspire 9.4 is the next minor version release of .NET Aspire. It supports:

- .NET 8.0 Long Term Support (LTS)
- .NET 9.0 Standard Term Support (STS)

If you have feedback, questions, or want to contribute to .NET Aspire, collaborate with us on [:::image type="icon" source="../media/github-mark.svg" border="false"::: GitHub](https://github.com/dotnet/aspire) or join us on [:::image type="icon" source="../media/discord-icon.svg" border="false"::: Discord](https://aka.ms/dotnet-discord) to chat with team members.

It's important to note that .NET Aspire releases out-of-band from .NET releases. While major versions of .NET Aspire align with major .NET versions, minor versions are released more frequently. For more information on .NET and .NET Aspire version support, see:

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy): Definitions for LTS and STS.
- [.NET Aspire support policy](https://dotnet.microsoft.com/platform/support/policy/aspire): Important unique product life cycle details.

## 🤖 Azure AI and ML enhancements

### ✨ Azure AI Foundry support

.NET Aspire 9.4 introduces first-class support for **Azure AI Foundry**, Microsoft's unified platform for AI development. You can now easily add AI Foundry resources and deployments to your Aspire applications:

```csharp
var aiFoundry = builder.AddAzureAIFoundry("ai-foundry");

var deployment = aiFoundry.AddDeployment("gpt-4", "gpt-4", "1106", "OpenAI");
```

This integration enables you to:

- **Deploy AI models** directly through Aspire's app model
- **Manage AI Foundry resources** alongside your other Azure services
- **Integrate AI capabilities** into your distributed applications with consistent configuration

## 🔐 Enhanced Azure Key Vault integration

### 🗝️ Simplified secret management

Azure Key Vault integration has been significantly enhanced with new `AddSecret` and `GetSecret` convenience methods, making secret management more intuitive:

```csharp
var keyVault = builder.AddAzureKeyVault("keyvault");

// Add secrets from parameters or reference expressions
var dbPassword = builder.AddParameter("db-password", secret: true);
keyVault.AddSecret("database-password", dbPassword);

// Retrieve secrets with the convenience API
var secret = keyVault.GetSecret("api-key");
```

The new API provides multiple overloads for adding secrets:

```csharp
// Add secret with parameter resource
keyVault.AddSecret("secret-name", parameterResource);

// Add secret with reference expression
keyVault.AddSecret("secret-name", referenceExpression);

// Add secret with custom resource name
keyVault.AddSecret("custom-name", "secret-name", parameterResource);
```

## 🗄️ Improved Azure Cosmos DB support

### 🌐 Hierarchical partition key support

Azure Cosmos DB containers now support **hierarchical partition keys**, enabling more efficient data distribution and querying:

```csharp
var cosmos = builder.AddAzureCosmosDB("cosmos");
var database = cosmos.AddCosmosDatabase("mydb");

// Support for multiple partition key paths
var container = database.AddContainer("products", 
    new[] { "/category", "/subcategory", "/brand" });
```

### ⚡ Serverless support

You can now enable serverless mode for Azure Cosmos DB accounts, perfect for applications with intermittent or unpredictable traffic:

```csharp
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .WithDefaultAzureSku(); // Enables serverless capability
```

## 💾 Azure Storage API improvements

### 🔄 Streamlined storage service APIs

The Azure Storage integration has been refactored to provide cleaner, more consistent APIs. The new methods replace the previous `AddBlobs`, `AddQueues`, and `AddTables` pattern:

```csharp
var storage = builder.AddAzureStorage("storage");

// New simplified service APIs
var blobService = storage.AddBlobService();
var queueService = storage.AddQueueService(); 
var tableService = storage.AddTableService();

// Direct container and queue creation
var container = storage.AddBlobContainer("uploads");
var queue = storage.AddQueue("notifications");
```

**Breaking Change**: The old `Add*` methods are now obsolete. Migrate to the new `Add*Service` methods for cleaner API surface.

## 🚀 CLI improvements

### ⚡ New `aspire exec` command (Preview)

The CLI now includes a preview `aspire exec` command that allows you to execute commands within the context of running Aspire applications:

```bash
# Execute commands in the context of your Aspire app
aspire exec --help
```

**Note**: This feature is behind a feature flag and disabled by default in this release.

### 🚀 Enhanced deployment capabilities

The `aspire deploy` command has been enhanced with better user experience and is also available as a preview feature:

```bash
# Deploy your Aspire application
aspire deploy --help
```

### 🎨 Improved CLI experience

Several CLI experience improvements have been added:

- **Purple styling** for default values in CLI prompts
- **Markup escaping** fixes for better rendering
- **User-friendly error handling** for `aspire new` when directories contain existing files
- **Enhanced template selection** with pre-release package support
- **Localization support** for better international user experience

```bash
# Improved new project experience
aspire new

# Enhanced package addition with better prompts
aspire add
```

## 🐳 Docker Compose enhancements

### 📊 Dashboard integration

Docker Compose environments now support integrated dashboard functionality:

```csharp
var dockerCompose = builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(enabled: true)
    .WithDashboard(dashboard => 
    {
        dashboard.WithHostPort(18888);
    });
```

### 📁 Enhanced file copying

New support for copying existing files via the `WithContainerFiles` API:

```csharp
var container = builder.AddContainer("myapp", "myimage")
    .WithContainerFiles("/source/path", "/dest/path");
```

## 🔧 Infrastructure improvements

### 🏷️ Consistent resource naming

All Azure resources now expose a `NameOutputReference` property for consistent resource naming across Azure services:

```csharp
var storage = builder.AddAzureStorage("storage");
var nameReference = storage.NameOutputReference; // Access the Azure resource name
```

This property is now available on:
- Azure App Configuration
- Azure App Containers  
- Azure Application Insights
- Azure Cosmos DB
- Azure Event Hubs
- Azure Key Vault
- Azure PostgreSQL
- Azure Redis
- Azure Search
- Azure Service Bus
- Azure SignalR
- Azure SQL
- Azure Storage
- Azure Web PubSub
- And many more Azure services

### 🎯 Emulator consistency

Azure emulator resources now include `EmulatorResourceAnnotation` for consistent tooling support across all emulator implementations.

## 💔 Breaking changes

### Azure Storage API changes

The Azure Storage extension methods have been updated for consistency:

**Before (.NET Aspire 9.3)**:
```csharp
var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");  
var tables = storage.AddTables("tables");
```

**After (.NET Aspire 9.4)**:
```csharp
var blobService = storage.AddBlobService("blobs");    // or AddBlobService() for default naming
var queueService = storage.AddQueueService("queues"); // or AddQueueService() for default naming
var tableService = storage.AddTableService("tables"); // or AddTableService() for default naming
```

The old methods are marked as obsolete and will be removed in a future release.

### Obsolete API cleanup

Several APIs have been marked as obsolete:

- `BicepSecretOutputReference` - Use `IAzureKeyVaultResource.GetSecret` instead
- `GetSecretOutput` - Use `IAzureKeyVaultResource.GetSecret` instead
- Various parameter APIs that are being consolidated

## 🔄 Database hosting improvements

### 📂 Improved initialization file support

Database hosting packages now provide cleaner APIs for initialization files:

**MongoDB**:
```csharp
var mongo = builder.AddMongoDB("mongo")
    .WithInitFiles("/path/to/init/scripts"); // Replaces WithInitBindMount
```

**MySQL**:
```csharp
var mysql = builder.AddMySql("mysql")
    .WithInitFiles("/path/to/init/scripts"); // Replaces WithInitBindMount
```

**PostgreSQL**:
```csharp
var postgres = builder.AddPostgreSQL("postgres")
    .WithInitFiles("/path/to/init/scripts"); // Replaces WithInitBindMount
```

**Oracle**:
```csharp
var oracle = builder.AddOracleDatabase("oracle")
    .WithInitFiles("/path/to/init/scripts"); // Replaces WithInitBindMount
```

The old `WithInitBindMount` methods are now obsolete.

## 🎛️ Advanced configuration improvements

### 🔧 Keycloak realm import simplification

Keycloak integration has been simplified for realm imports:

```csharp
var keycloak = builder.AddKeycloak("keycloak")
    .WithRealmImport("/path/to/realm.json"); // Simplified API without isReadOnly parameter
```

### 🗂️ Milvus configuration improvements

Milvus hosting now provides cleaner configuration file management:

```csharp
var milvus = builder.AddMilvus("milvus")
    .WithConfigurationFile("/path/to/milvus.yaml"); // Replaces WithConfigurationBindMount
```

## 🌐 YARP integration enhancements

### 🔄 Enhanced YARP configuration

The YARP integration has been enhanced with new configuration builder APIs for better route and cluster management. You can now create sophisticated reverse proxy configurations using the fluent API:

```csharp
// Add YARP proxy to your application
var yarp = builder.AddYarp("gateway")
    .WithHostPort(8080);

// Configure routes and clusters
yarp.WithConfiguration(config =>
{
    // Create clusters from existing resources
    var apiCluster = config.AddCluster(apiService);
    var webCluster = config.AddCluster(webService);
    
    // Add routes with specific paths
    config.AddRoute("/api/*", apiCluster);
    config.AddRoute("/", webCluster);
});
```

The new `IYarpConfigurationBuilder` interface provides multiple ways to configure clusters and routes:

```csharp
yarp.WithConfiguration(config =>
{
    // Add cluster from endpoint reference
    var cluster1 = config.AddCluster(someEndpoint);
    
    // Add cluster from external service
    var cluster2 = config.AddCluster(externalServiceBuilder);
    
    // Add cluster from any resource with service discovery
    var cluster3 = config.AddCluster(resourceBuilder);
    
    // Create routes with various patterns
    config.AddRoute("/api/v1/*", cluster1);
    config.AddRoute(someEndpoint);           // Direct endpoint routing
    config.AddRoute(externalServiceBuilder); // External service routing
});
```

This enhancement makes it much easier to set up complex routing scenarios in your distributed applications with clean, declarative configuration.

### ⚙️ Advanced YARP cluster configuration

YARP clusters now support extensive configuration options for fine-tuning reverse proxy behavior:

```csharp
yarp.WithConfiguration(config =>
{
    var cluster = config.AddCluster(backendService)
        .WithLoadBalancingPolicy("PowerOfTwoChoices")
        .WithHealthCheckConfig(new HealthCheckConfig 
        { 
            Active = new ActiveHealthCheckConfig 
            { 
                Enabled = true, 
                Interval = TimeSpan.FromSeconds(30) 
            } 
        })
        .WithHttpClientConfig(new HttpClientConfig 
        { 
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            MaxConnectionsPerServer = 100
        })
        .WithSessionAffinityConfig(new SessionAffinityConfig 
        { 
            Enabled = true, 
            Policy = "Cookie" 
        });
});
```

## 🔐 Enhanced Azure identity and security

### 🆔 User-assigned managed identity support

You can now easily configure Azure resources with user-assigned managed identities for enhanced security:

```csharp
var identity = builder.AddAzureUserAssignedIdentity("app-identity");

// Apply the identity to compute resources
var containerApp = builder.AddAzureContainerApp("api")
    .WithAzureUserAssignedIdentity(identity);
```

This feature works with any `IComputeResource` and provides consistent identity management across your Azure infrastructure.

## 📊 Enhanced Azure monitoring and observability

### 📈 Improved Application Insights integration

Azure Application Insights now supports flexible Log Analytics workspace configuration:

```csharp
var workspace = builder.AddAzureLogAnalyticsWorkspace("workspace");
var appInsights = builder.AddAzureApplicationInsights("insights")
    .WithLogAnalyticsWorkspace(workspace);

// Or use an existing workspace reference
var appInsights2 = builder.AddAzureApplicationInsights("insights2")
    .WithLogAnalyticsWorkspace(existingWorkspaceId);
```

### 📊 Container Apps dashboard support

Azure Container App environments now include built-in dashboard support and enhanced Log Analytics integration:

```csharp
var workspace = builder.AddAzureLogAnalyticsWorkspace("workspace");

// Create a container app environment with dashboard enabled
var containerEnv = builder.AddAzureContainerAppEnvironment("container-env")
    .WithAzureLogAnalyticsWorkspace(workspace)
    .WithDashboard(enable: true);
```

## 🏗️ Enhanced Azure infrastructure capabilities

### 🏢 Azure Compute Environment interface

A new `IAzureComputeEnvironmentResource` interface provides a consistent abstraction for Azure compute environments, implemented by:

- `AzureContainerAppEnvironmentResource`
- `AzureAppServiceEnvironmentResource`

This enables better tooling and consistent patterns across different Azure compute platforms.

### 📦 Enhanced Azure Queue Storage

Azure Storage now supports direct queue creation with full connection string support:

```csharp
var storage = builder.AddAzureStorage("storage");

// Create individual queues directly
var notificationQueue = storage.AddQueue("notifications", "notification-queue");
var processingQueue = storage.AddQueue("processing", "task-processing");

// Each queue has its own connection string
var queueConnection = notificationQueue.ConnectionStringExpression;
```

## 🔧 Advanced Kubernetes enhancements

### 📋 Additional Kubernetes resources

Kubernetes resources now support additional resource management through the `AdditionalResources` property:

```csharp
var k8sResource = builder.AddKubernetesResource("myapp");
// Access additional Kubernetes resources that can be deployed alongside the main resource
var additionalResources = k8sResource.Resource.AdditionalResources;
```

### 🏗️ Workload abstraction improvements

The Kubernetes hosting package now provides a cleaner `Workload` abstraction that supports both `Deployment` and `StatefulSet` resources with consistent `PodTemplate` access.

## 📁 Enhanced Docker Compose capabilities

### ⚙️ Configuration management

Docker Compose now supports advanced configuration management:

```csharp
var compose = builder.AddDockerComposeEnvironment("app")
    .ConfigureComposeFile(composeFile =>
    {
        // Add configurations with content
        var config = new Config 
        { 
            Content = "app.setting=value\nother.setting=true",
            Mode = UnixFileMode.UserRead | UnixFileMode.GroupRead
        };
        composeFile.AddConfig(config);
        
        // Services can reference configurations
        composeFile.Services["web"].AddConfig(new ConfigReference 
        { 
            Source = "app-config",
            Target = "/app/config.properties",
            Mode = UnixFileMode.UserRead
        });
    });
```

---

This release represents a significant step forward in .NET Aspire's capabilities, with major improvements to Azure service integration, CLI tooling, and developer experience. The enhanced Key Vault integration, new Azure AI Foundry support, and improved storage APIs make it easier than ever to build sophisticated cloud-native applications with .NET Aspire.
