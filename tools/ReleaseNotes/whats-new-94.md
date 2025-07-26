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

### 🔐 Enhanced Azure security with disabled local authentication

.NET Aspire 9.4 automatically disables local authentication for Azure EventHubs and Azure Web PubSub resources, enforcing the use of managed identity for enhanced security by default.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Azure EventHubs with automatic local auth disabled
var eventHubs = builder.AddAzureEventHubs("eventhubs");
var hub = eventHubs.AddEventHub("orders");

// Azure Web PubSub with automatic local auth disabled  
var webPubSub = builder.AddAzureWebPubSub("webpubsub");

// Services connect using managed identity automatically
var processor = builder.AddProject<Projects.EventProcessor>("processor")
    .WithReference(hub)
    .WithReference(webPubSub);

builder.Build().Run();
```

**Security improvements:**
- **Local authentication disabled** by default on Azure EventHubs and Web PubSub
- **Managed identity enforcement** - applications must use Azure AD-based authentication
- **Reduced attack surface** - eliminates connection string-based authentication vulnerabilities
- **Zero configuration required** - security enhancement is applied automatically
- **Compliance alignment** - meets enterprise security requirements for password-less authentication

**Key benefits:**
- **No breaking changes** - existing applications continue to work with managed identity
- **Enhanced security posture** - eliminates risks associated with connection string leaks
- **Simplified credential management** - no secrets to rotate or manage
- **Audit trail improvements** - all access is tied to Azure AD identities

This change automatically applies to all Azure EventHubs and Web PubSub resources created through Aspire, ensuring secure-by-default behavior without requiring additional configuration.

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
var builder = WebApplication.CreateBuilder(args);

// Azure Table Storage with new standardized naming
builder.AddAzureTableServiceClient("tables");
builder.AddKeyedAzureTableServiceClient("primary-tables");

// Azure Queue Storage with consistent API
builder.AddAzureQueueServiceClient("queues");
builder.AddKeyedAzureQueueServiceClient("primary-queues");

// Keyed Blob Storage (enhanced support)
builder.AddKeyedAzureBlobServiceClient("primary-blobs");

var app = builder.Build();
```

The new client registration methods provide consistent naming across all Azure Storage services and full support for keyed dependency injection scenarios.

### 📡 Enhanced tracing configuration

Azure components now support granular tracing control with new `DisableTracing` options:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Azure App Configuration with tracing disabled
builder.AddAzureAppConfiguration("config", settings =>
{
    settings.DisableTracing = true; // Disable OpenTelemetry tracing
});

// Other Azure services also support this configuration
builder.AddAzureKeyVaultClient("vault", settings =>
{
    settings.DisableTracing = true;
});

builder.AddAzureServiceBusClient("servicebus", settings =>
{
    settings.DisableTracing = true;
});

var app = builder.Build();
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
                    .WithDashboard(dashboard => dashboard.WithHostPort(8080)); // Configure dashboard with specific port

// Add services that will automatically report to the dashboard
builder.AddProject<Projects.Frontend>("frontend");
builder.AddProject<Projects.Api>("api");

builder.Build().Run();
```

**Key capabilities:**
- **Dashboard integration** automatically configures OTLP endpoints and service discovery
- **Port configuration** with `WithHostPort()` for predictable dashboard access
- **Same observability experience** in production Docker environments as local development
- **Seamless service discovery** for all containerized resources
- **Improved security** with selective port exposure - only external endpoints are mapped to host ports

### 🔒 Enhanced Docker Compose security

.NET Aspire 9.4 improves Docker Compose security by implementing smarter port mapping behavior. The system now distinguishes between internal and external endpoints, only exposing truly external endpoints via port mappings while using Docker Compose's `expose` directive for internal service-to-service communication.

**Security improvements:**
- **Selective port exposure** - Only endpoints marked as external are exposed to the host
- **Internal service isolation** - Internal endpoints use Docker's `expose` for container-to-container communication
- **Reduced attack surface** - Fewer ports exposed to the host system
- **Better network hygiene** - Clear separation between public and private endpoints

This change enhances the security posture of Docker Compose deployments by ensuring that internal services are not inadvertently exposed to the host network, while maintaining proper connectivity for legitimate external access points.

### 🔄 Resource lifecycle events

.NET Aspire 9.4 introduces **convenient extension methods** on `IResourceBuilder<T>` that make it much easier to subscribe to lifecycle events directly on resources, providing a cleaner and more intuitive API.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres");

var api = builder.AddProject<Projects.Api>("api")
                .WithReference(database)
                .OnInitializeResource(async (resource, evt, cancellationToken) =>
                {
                    // Early resource initialization
                    var logger = evt.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Initializing resource {Name}", resource.Name);
                })
                .OnBeforeResourceStarted(async (resource, evt, cancellationToken) =>
                {
                    // Pre-startup validation or configuration
                    var serviceProvider = evt.Services;
                    // Additional validation logic here
                })
                .OnConnectionStringAvailable(async (resource, evt, cancellationToken) =>
                {
                    // Log when connection strings are resolved
                    var logger = evt.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Connection string available for {Name}", resource.Name);
                })
                .OnResourceEndpointsAllocated(async (resource, evt, cancellationToken) =>
                {
                    // React to endpoint allocation
                    var logger = evt.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Endpoints allocated for {Name}", resource.Name);
                })
                .OnResourceReady(async (resource, evt, cancellationToken) =>
                {
                    // Resource is fully ready
                    var logger = evt.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Resource {Name} is ready", resource.Name);
                });

// Example: Database seeding using OnResourceReady
var db = builder.AddMongoDB("mongo")
    .WithMongoExpress()
    .AddDatabase("db")
    .OnResourceReady(async (db, evt, ct) =>
    {
        // Seed the database with initial data
        var connectionString = await db.ConnectionStringExpression.GetValueAsync(ct);
        using var client = new MongoClient(connectionString);
        
        var myDb = client.GetDatabase("db");
        await myDb.CreateCollectionAsync("entries", cancellationToken: ct);
        
        // Insert sample data
        for (int i = 0; i < 10; i++)
        {
            await myDb.GetCollection<Entry>("entries").InsertOneAsync(new Entry(), cancellationToken: ct);
        }
    });

builder.Build().Run();
```

**Available lifecycle events:**
- **`OnInitializeResource()`** - Called during early resource initialization
- **`OnBeforeResourceStarted()`** - Called before the resource starts
- **`OnConnectionStringAvailable()`** - Called when connection strings are resolved (requires `IResourceWithConnectionString`)
- **`OnResourceEndpointsAllocated()`** - Called when resource endpoints are allocated (requires `IResourceWithEndpoints`)
- **`OnResourceReady()`** - Called when the resource is fully ready

**Key improvements in .NET Aspire 9.4:**
- **Fluent API** - Chain event subscriptions directly on resource builders for cleaner code
- **Strongly-typed callbacks** - Each event method provides the specific resource type and event type
- **Type constraints** - Methods are only available on appropriate resource types (e.g., `OnConnectionStringAvailable` only on resources with connection strings)
- **Simplified syntax** - No need to manually subscribe to the eventing system or handle resource matching
- **Better IntelliSense** - Full IDE support with proper type checking and auto-completion

**Migration from manual eventing:**
```csharp
// ❌ Before (manual eventing subscription):
builder.Eventing.Subscribe<ResourceReadyEvent>(db.Resource, async (evt, ct) =>
{
    // Manual event handling with no type safety
    var cs = await db.Resource.ConnectionStringExpression.GetValueAsync(ct);
    // Process event...
});

// ✅ After (fluent extension methods):
var db = builder.AddMongoDB("mongo")
    .AddDatabase("db")
    .OnResourceReady(async (db, evt, ct) =>
    {
        // Direct access to strongly-typed resource
        var cs = await db.ConnectionStringExpression.GetValueAsync(ct);
        // Process event...
    });
```

These events enable sophisticated startup orchestration, health validation, and initialization workflows without requiring complex external coordination mechanisms. The new extension methods make it much easier to implement common patterns like database seeding, configuration validation, and resource health checks.

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

### 🔗 Enhanced endpoint URL support

.NET Aspire 9.4 introduces enhanced support for non-localhost URLs, making it easier to work with custom domains and network configurations. This includes support for `*.localhost` subdomains and automatic generation of multiple URL variants for endpoints listening on multiple addresses.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Endpoints targeting all addresses automatically get multiple URL variants
var api = builder.AddProject<Projects.Api>("api")
    .WithEndpoint("https", e => e.TargetHost = "0.0.0.0");

// Machine name URLs for external access  
var publicService = builder.AddProject<Projects.PublicService>("public")
    .WithEndpoint("https", e => e.TargetHost = "0.0.0.0");

builder.Build().Run();
```

**Key capabilities:**
- **`*.localhost` subdomain support** - Use custom subdomains while maintaining localhost behavior
- **Multiple URL generation** - Endpoints listening on multiple addresses automatically get localhost and machine name URLs
- **Enhanced dashboard integration** - All URL variants appear in the Aspire dashboard for easy access
- **Network flexibility** - Better support for development scenarios requiring specific network configurations
- **Launch profile configuration** - For projects, custom URLs can also be configured via launch profiles in `launchSettings.json`

**Launch profile configuration example:**
```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "applicationUrl": "https://*:7001;http://*:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

This enhancement simplifies development workflows where custom domains or external network access is needed while maintaining the familiar localhost development experience.

### 📋 Parameters and connection strings visible in dashboard

.NET Aspire 9.4 makes parameters and connection strings visible in the Aspire dashboard, providing better visibility into your application's configuration and connectivity status during development.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Parameters now appear in the dashboard
var apiKey = builder.AddParameter("api-key", secret: true);
var connectionString = builder.AddParameter("connection-string", secret: true);

// Connection strings are also visible with status
var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("mydb");

var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("API_KEY", apiKey)
    .WithReference(database);

builder.Build().Run();
```

**Key benefits:**
- **Parameter visibility** - All parameters appear in the dashboard with their resolved state
- **Connection string display** - Full connection strings are now shown in resource details as a sensitive property
- **Real-time updates** - Connection strings automatically appear when resources become available
- **Configuration debugging** - Easily verify that parameters are correctly resolved and available
- **Development experience** - No more guessing about configuration state during development

**Connection string details:**
- Connection strings appear in the **resource details** panel for any resource that implements `IResourceWithConnectionString`
- Values are marked as **sensitive** and can be toggled for visibility in the dashboard
- The system automatically listens for `ConnectionStringAvailableEvent` and updates the dashboard in real-time
- Supports all resource types including databases, message brokers, and custom resources

This enhancement removes the previous hidden status of parameters and connection strings, making configuration state transparent and easier to debug.

### 🔗 Enhanced dashboard peer visualization for uninstrumented resources

.NET Aspire 9.4 introduces advanced peer visualization capabilities in the dashboard, enabling you to see connections between resources even when they aren't instrumented with telemetry. This includes support for parameters, connection strings, GitHub Models, and external services.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Parameters with URLs/connection strings are automatically visualized
var apiUrl = builder.AddParameter("api-url")
    .WithDescription("External API endpoint URL");

var dbConnectionString = builder.AddParameter("database-url", secret: true)
    .WithDescription("External database connection string");

// GitHub Models show peer connections
var apiKey = builder.AddParameter("github-api-key", secret: true);
var organization = builder.AddParameter("github-org");

var model = builder.AddGitHubModel("chat-model", "gpt-4o-mini", organization)
    .WithApiKey(apiKey)
    .WithHealthCheck();

// External services with connection string references
var externalDb = builder.AddExternalService("external-db", dbConnectionString)
    .WithHttpHealthCheck("/health");

var externalApi = builder.AddExternalService("external-api", apiUrl);

// Dashboard visualizes all these connections automatically
var myService = builder.AddProject<Projects.MyService>("my-service")
    .WithReference(model)         // Shows connection to GitHub Models
    .WithReference(externalDb)    // Shows connection to external database
    .WithReference(externalApi)   // Shows connection to external API
    .WithEnvironment("DB_URL", dbConnectionString);  // Shows parameter usage

builder.Build().Run();
```

**Enhanced visualization capabilities:**
- **Connection string parsing** - Comprehensive parser supports SQL Server, PostgreSQL, MySQL, MongoDB, Redis, and many other connection string formats
- **Parameter visualization** - Shows how parameters with URLs or connection strings connect to services
- **GitHub Models integration** - Visualizes connections to GitHub-hosted AI models with proper state management
- **External service mapping** - Shows relationships between your services and external dependencies
- **Uninstrumented peer detection** - Identifies connections even without OpenTelemetry instrumentation

**Supported connection string formats:**
- **Database connections** - SQL Server, PostgreSQL, MySQL, MongoDB, Oracle, SQLite
- **Cache connections** - Redis, Memcached
- **Message brokers** - Azure Service Bus, RabbitMQ, Apache Kafka
- **HTTP/API endpoints** - REST APIs, GraphQL endpoints, webhooks
- **Generic URL connections** - Any HTTP/HTTPS endpoint with automatic host/port extraction

**Key benefits:**
- **Complete dependency mapping** - See all service connections, not just instrumented ones
- **Better debugging** - Understand how services connect to external dependencies
- **Improved observability** - Visualize the complete application architecture
- **Enhanced troubleshooting** - Identify connection issues and dependency problems
- **Performance optimization** - Resource address caching improves dashboard responsiveness

This enhancement makes the Aspire dashboard significantly more powerful for understanding complex distributed applications, especially those integrating with external services and legacy systems that may not have full telemetry instrumentation.

**GitHub Issue:** [#10382](https://github.com/dotnet/aspire/issues/10382)

### 📋 Console logs text wrapping control

.NET Aspire 9.4 introduces a new toggle option in the dashboard console logs to control text wrapping behavior, giving you better control over how long log lines are displayed.

**Key features:**
- **Wrap toggle control** - New menu button to enable/disable text wrapping for log lines
- **Persistent settings** - Wrap preference is saved in browser storage and restored between sessions
- **Visual indicators** - Clear icons show current wrap state (text wrap on/off)
- **Improved readability** - Choose between wrapped text for readability or unwrapped for preserving original formatting
- **Flexible viewing** - Switch between modes based on log content and debugging needs

**Benefits:**
- **Better log analysis** - Choose the best viewing mode for different types of log content
- **Preserved formatting** - Unwrapped mode maintains original log line structure for formatted output
- **Enhanced readability** - Wrapped mode prevents horizontal scrolling for long lines
- **User preference** - Customizable viewing experience saved across dashboard sessions

This enhancement improves the console logs viewing experience by providing developers with control over how log content is displayed, making it easier to debug applications with varying log line lengths and formats.

**GitHub Issue:** [#10314](https://github.com/dotnet/aspire/issues/10314)

### 👁️ Show/hide hidden resources in dashboard

.NET Aspire 9.4 introduces the ability to show or hide hidden resources in the dashboard, giving you complete visibility into your application's infrastructure components and internal resources that are normally hidden from view.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// These infrastructure components are normally hidden
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin(); // PgAdmin is typically hidden

var redis = builder.AddRedis("redis");

// DCP/Orchestrator components are hidden by default
var app = builder.AddProject<Projects.MyApp>("myapp")
    .WithReference(postgres)
    .WithReference(redis);

builder.Build().Run();
```

**Key capabilities:**
- **Toggle hidden resources** - New menu button to show/hide infrastructure and internal components
- **Persistent preference** - Show/hide setting is saved in browser storage and restored between sessions
- **Smart discovery** - Automatically detects when hidden resources exist in your application
- **Complete visibility** - View all infrastructure components, orchestrator resources, and internal services
- **Conditional display** - Menu option only appears when hidden resources are actually present

**Hidden resource types include:**
- **Infrastructure components** - Database admin tools (PgAdmin, phpMyAdmin, Redis Commander)
- **Orchestrator resources** - DCP (Developer Control Plane) internal components
- **Internal services** - Background services and worker processes
- **Management tools** - Monitoring and management interfaces
- **System resources** - Framework-level infrastructure components

**Benefits:**
- **Enhanced debugging** - See all components involved in your application's operation
- **Infrastructure awareness** - Understand the complete resource topology
- **Troubleshooting support** - Access to normally hidden components for diagnostics
- **Development insights** - Better understanding of how Aspire orchestrates your application
- **Resource management** - Complete view of all running services and dependencies

**Usage:**
- **Dashboard menu** - Use the eye icon in the resources page menu to toggle hidden resource visibility
- **Console logs** - Hidden resources also appear in console log resource selection when enabled
- **Resource graph** - Hidden resources are included in the visual resource dependency graph

This feature is particularly useful for debugging complex applications, understanding infrastructure dependencies, and gaining insights into how Aspire manages your distributed application's complete resource topology.

**GitHub Issue:** [#9180](https://github.com/dotnet/aspire/issues/9180)

### 🏗️ Enhanced dashboard infrastructure with proxied endpoints

.NET Aspire 9.4 introduces significant infrastructure improvements to the dashboard system, implementing proxied endpoints that make dashboard launching more reliable by fixing port reuse problems. This architectural enhancement resolves issues with dashboard connectivity during application startup and shutdown scenarios.

**Key improvements:**
- **Proxied endpoint architecture** - Dashboard endpoints are now modeled as first-class proxied resources in the DCP (Developer Control Plane)
- **Startup retry resilience** - Automatic retry handling during DCP proxy startup eliminates connection failures from unclean dashboard shutdowns
- **Port reuse problem resolution** - Fixes issues where dashboard ports weren't properly released from previous runs
- **Improved reliability** - Better handling of cases where the dashboard wasn't cleanly shut down from a previous run

**Technical benefits:**
- **Unified endpoint model** - OTLP (OpenTelemetry Protocol) endpoints for both gRPC and HTTP are now consistently managed
- **Target URL resolution** - Proper handling of target host and port configurations for proxied scenarios
- **Environment variable optimization** - Streamlined configuration of `ASPNETCORE_URLS` and OTLP endpoint URLs
- **Reference expression support** - Dashboard URLs are now properly handled through the reference expression system

This infrastructure enhancement provides a more robust foundation for dashboard operations, particularly in complex development environments and deployment scenarios where network connectivity can be challenging. The primary benefit is making dashboard launching more reliable by eliminating the common port reuse issues that could prevent the dashboard from starting after previous application runs.

**GitHub Issue:** [#10587](https://github.com/dotnet/aspire/issues/10587)

### 🔄 Interactive parameter prompting during run mode

.NET Aspire 9.4 introduces interactive parameter prompting, automatically collecting missing parameter values during application startup through the dashboard interface.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Parameters without default values will trigger prompts
var apiKey = builder.AddParameter("api-key", secret: true);
var dbPassword = builder.AddParameter("db-password", secret: true);  
var environment = builder.AddParameter("environment");

// Application will prompt for these values if not provided
var database = builder.AddPostgres("postgres", password: dbPassword);
var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("API_KEY", apiKey)
    .WithEnvironment("ENVIRONMENT", environment)
    .WithReference(database);

builder.Build().Run();
```

**Interactive experience:**
- **Automatic detection** - Aspire detects missing parameter values during startup
- **Dashboard prompts** - Interactive forms appear in the dashboard for parameter collection
- **Validation support** - Parameters can include validation rules and helpful descriptions
- **Secure handling** - Secret parameters are properly masked during input
- **Persistent storage** - Collected values can be saved to user secrets for future runs

**Key improvements:**
- **No more startup failures** - Missing parameters trigger prompts instead of errors
- **Developer-friendly** - Clean interface for providing configuration values
- **Security-conscious** - Secret parameters are handled appropriately
- **Validation feedback** - Clear error messages for invalid parameter values

This feature eliminates the need to pre-configure all parameters before running your Aspire application, making the development experience more fluid and user-friendly.

### 📝 Enhanced parameter descriptions and custom input rendering

Building on the interactive parameter prompting capabilities, .NET Aspire 9.4 introduces rich parameter descriptions and custom input rendering to provide better user guidance and specialized input controls during parameter collection.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Parameters with descriptions provide better user guidance
var apiKey = builder.AddParameter("api-key", secret: true)
    .WithDescription("API key for external service authentication");

var environment = builder.AddParameter("environment")
    .WithDescription("Target deployment environment (dev, staging, prod)");

// Parameters with rich markdown descriptions
var configValue = builder.AddParameter("config-value")
    .WithDescription("""
        Configuration value with detailed instructions:
        
        - Use **development** for local testing
        - Use **staging** for pre-production validation  
        - Use **production** for live deployments
        
        See [configuration guide](https://docs.company.com/config) for details.
        """, enableMarkdown: true);

// Custom input rendering for specialized scenarios
var workerCount = builder.AddParameter("worker-count")
    .WithDescription("Number of background worker processes")
    .WithCustomInput(p => new InteractionInput
    {
        InputType = InputType.Number,
        Label = "Worker Count",
        Placeholder = "Enter number (1-10)",
        Description = p.Description,
        Minimum = 1,
        Maximum = 10
    });

var deploymentRegion = builder.AddParameter("region")
    .WithDescription("Azure region for deployment")
    .WithCustomInput(p => new InteractionInput
    {
        InputType = InputType.Choice,
        Label = "Deployment Region",
        Description = p.Description,
        Choices = new[] { "East US", "West US", "North Europe", "Southeast Asia" }
    });

var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("API_KEY", apiKey)
    .WithEnvironment("ENVIRONMENT", environment)
    .WithEnvironment("CONFIG_VALUE", configValue)
    .WithEnvironment("WORKER_COUNT", workerCount)
    .WithEnvironment("REGION", deploymentRegion);

builder.Build().Run();
```

**Enhanced parameter capabilities:**
- **`WithDescription()`** - Add helpful descriptions to guide users during parameter input
- **Markdown support** - Rich text descriptions with links, formatting, and lists using `enableMarkdown: true`
- **`WithCustomInput()`** - Create specialized input controls for specific parameter types
- **Enhanced validation** - Better feedback for invalid parameter values with min/max constraints
- **Improved UX** - More intuitive parameter collection with contextual help

**Supported input types for custom rendering:**
- `Text` - Standard text input with placeholder support
- `SecretText` - Password/secret input (masked)
- `Number` - Numeric input with min/max validation
- `Boolean` - Checkbox input for true/false values
- `Choice` - Dropdown selection from predefined options

**Key benefits:**
- **Better user guidance** - Clear descriptions explain what each parameter is for
- **Rich documentation** - Markdown support allows for formatted help text with links
- **Appropriate input controls** - Numbers use numeric inputs, choices use dropdowns
- **Validation feedback** - Clear constraints and error messages
- **Professional appearance** - Well-formatted prompts improve the development experience

This enhancement makes parameter collection more intuitive and user-friendly, reducing confusion and errors during application startup while providing professional-quality input forms in the Aspire dashboard.

**GitHub Issue:** [#10447](https://github.com/dotnet/aspire/issues/10447)

### 🐳 Enhanced persistent container support

.NET Aspire 9.4 improves support for persistent containers with better lifecycle management and networking capabilities, ensuring containers can persist across application restarts while maintaining proper connectivity.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Persistent containers with improved lifecycle support
var database = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithExplicitStart(); // Better support for explicit start with persistent containers

// Persistent containers automatically get persistent networking
var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(database)
    .WithReference(redis);

builder.Build().Run();
```

**Enhanced capabilities:**
- **Improved lifecycle management** - Better coordination between `WithExplicitStart()` and `ContainerLifetime.Persistent`
- **Persistent networking** - Automatic persistent network creation when persistent containers are detected
- **Container delay start** - Uses orchestrator container delay start feature for more reliable startup sequencing
- **Network isolation** - Persistent and session-scoped containers use separate networks for better resource management

**Key benefits:**
- **Data persistence** - Database and cache containers maintain state across application restarts
- **Development efficiency** - No need to repeatedly seed databases or warm caches
- **Network stability** - Consistent network topology for persistent infrastructure
- **Resource optimization** - Separate networks prevent conflicts between persistent and ephemeral resources

This enhancement provides a more robust foundation for development scenarios requiring stateful services that persist beyond individual application runs.

### 🎛️ Enhanced resource command service

.NET Aspire 9.4 introduces a centralized `ResourceCommandService` for executing commands against resources, along with enhanced APIs for adding custom commands to your resources. You can now easily add custom commands that appear in the Aspire dashboard and can be executed programmatically.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgres("postgres")
    .WithHttpCommand("admin-restart", "Restart Database", 
        commandName: "db-restart",
        commandOptions: new HttpCommandOptions
        {
            Method = HttpMethod.Post,
            Description = "Restart the PostgreSQL database"
        });

var cache = builder.AddRedis("cache")
    .WithHttpCommand("admin-flush", "Flush Cache",
        commandName: "cache-flush",
        commandOptions: new HttpCommandOptions
        {
            Method = HttpMethod.Delete,
            Description = "Clear all cached data"
        });

// Add a composite command that coordinates multiple operations
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(database)
    .WithReference(cache)
    .WithCommand("reset-all", "Reset Everything", async (context, ct) =>
    {
        var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var commandService = context.ServiceProvider.GetRequiredService<ResourceCommandService>();
        
        logger.LogInformation("Starting full system reset...");
        
        try
        {
            // Execute the cache flush command by name
            var flushResult = await commandService.ExecuteCommandAsync(cache.Resource, "cache-flush", ct);
            if (!flushResult.Success)
            {
                return CommandResults.Failure($"Failed to flush cache: {flushResult.ErrorMessage}");
            }
            
            // Execute the database restart command by name
            var restartResult = await commandService.ExecuteCommandAsync(database.Resource, "db-restart", ct);
            if (!restartResult.Success)
            {
                return CommandResults.Failure($"Failed to restart database: {restartResult.ErrorMessage}");
            }
            
            logger.LogInformation("System reset completed successfully");
            return CommandResults.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "System reset failed");
            return CommandResults.Failure(ex);
        }
    },
    updateState: (context) => 
    {
        // Only enable when all dependencies are running
        return context.ResourceSnapshot.State == KnownResourceStates.Running 
            ? ResourceCommandState.Enabled 
            : ResourceCommandState.Disabled;
    },
    displayDescription: "Reset cache and restart database in coordinated sequence",
    confirmationMessage: "This will clear all cached data and restart the database. Continue?",
    iconName: "ArrowClockwise",
    iconVariant: IconVariant.Filled,
    isHighlighted: true);

builder.Build().Run();
```

**Key capabilities:**
- **Fluent command APIs** - Add commands directly to resource builders with `WithCommand()`
- **HTTP command support** - Built-in `WithHttpCommand()` for REST API-based operations
- **Dashboard integration** - All commands appear in the Aspire dashboard with custom icons and descriptions
- **State management** - Commands can be enabled/disabled based on resource state
- **Confirmation dialogs** - Built-in support for confirmation prompts for destructive operations
- **Programmatic execution** - Execute any command through `ResourceCommandService`

**Command customization options:**
- **Custom icons** - Choose from built-in Fluent UI icons or use custom variants
- **Descriptions and help text** - Provide contextual information for each command
- **State-based availability** - Enable/disable commands based on resource state
- **Confirmation prompts** - Require user confirmation for potentially dangerous operations
- **Highlighting** - Mark important commands as highlighted in the dashboard

**Common use cases:**
- **Container restarts** - Restart specific containers or services
- **Cache operations** - Clear cache, warm up data, or refresh content
- **Database maintenance** - Run migrations, backups, or cleanup operations
- **Service management** - Reload configurations, restart workers, or trigger deployments
- **Debugging operations** - Run diagnostic commands, dump logs, or collect metrics
- **Testing scenarios** - Trigger test data setup, reset test environments, or execute test operations

This enhancement provides a much more intuitive way to add operational commands to your resources, making them easily accessible through both the dashboard UI and programmatic APIs.

**Testing scenario example:**

```csharp
[Fact]
public async Task Should_ResetCache_WhenTestStarts()
{
    var builder = DistributedApplication.CreateBuilder();
    
    // Add cache with reset command for testing
    var cache = builder.AddRedis("test-cache")
        .WithHttpCommand("reset", "Reset Cache",
            commandName: "reset-cache",
            commandOptions: new HttpCommandOptions
            {
                Method = HttpMethod.Delete,
                Description = "Clear all cached test data"
            });

    var api = builder.AddProject<Projects.TestApi>("test-api")
        .WithReference(cache);

    await using var app = builder.Build();
    await app.StartAsync();
    
    // Reset cache before running test
    var result = await app.ResourceCommands.ExecuteCommandAsync(
        cache.Resource, 
        "reset-cache", 
        CancellationToken.None);
        
    Assert.True(result.Success, $"Failed to reset cache: {result.ErrorMessage}");
    
    // Now run your test with a clean cache
    // ... test logic here
}
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

### ⚙️ Enhanced Azure provisioning interaction

.NET Aspire 9.4 significantly improves the Azure provisioning experience by leveraging the interaction services to streamline Azure subscription and resource group configuration during deployment workflows.

The enhanced Azure provisioning system:

- **Automatically prompts for missing Azure configuration** during deploy operations
- **Saves configuration to user secrets** for future deployments
- **Provides smart defaults** like auto-generated resource group names
- **Includes validation callbacks** for Azure-specific inputs like subscription IDs and locations
- **Supports rich HTML prompts** with links to create free Azure accounts

**Key improvements:**
- **Streamlined first-time setup** - No more manual configuration of Azure parameters
- **Persistent settings** - Configuration is saved securely in user secrets
- **Context-aware prompts** - Only prompts for missing configuration
- **Enhanced validation** - Built-in validation for Azure resource constraints
- **Better error handling** - Clear feedback when provisioning fails

This enhancement makes Azure deployment significantly more user-friendly, especially for developers new to Azure or setting up projects for the first time. The interaction system ensures that all necessary Azure configuration is collected interactively and stored securely for subsequent deployments.

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

### 🛡️ Automatic DataProtection configuration for scaled applications

.NET Aspire 9.4 automatically configures DataProtection for .NET projects deployed to Azure Container Apps, ensuring applications work correctly when scaling beyond a single instance.

**Background**: When ASP.NET Core applications scale to multiple instances, they need shared DataProtection keys to decrypt cookies, authentication tokens, and other protected data across all instances. Without proper configuration, users experience authentication issues and data corruption when load balancers route requests to different container instances.

**Automatic configuration**: .NET Aspire now automatically enables `autoConfigureDataProtection` for all .NET projects deployed to Azure Container Apps:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("production");

// DataProtection is automatically configured for scaling
var api = builder.AddProject<Projects.WebApi>("api");

var frontend = builder.AddProject<Projects.BlazorApp>("frontend");

builder.Build().Run();
```

**Key benefits**:
- **Seamless scaling** - Applications work correctly when Azure automatically scales to multiple instances
- **Consistent user experience** - No authentication issues or session loss when requests hit different instances  
- **Zero configuration required** - DataProtection is automatically configured using Azure Container Apps managed identity
- **Production-ready security** - Uses Azure Key Vault integration provided by the platform

This enhancement aligns Aspire-generated deployments with Azure Developer CLI (`azd`) behavior and resolves common production scaling issues without requiring manual DataProtection configuration.

### 🐳 Azure App Service container support

.NET Aspire 9.4 introduces support for deploying containerized applications with Dockerfiles to Azure App Service environments. This enables a seamless transition from local container development to Azure App Service deployment.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Create an Azure App Service environment
builder.AddAzureAppServiceEnvironment("app-service-env");

// Add a containerized project with Dockerfile
var containerApp = builder.AddContainer("my-app", "my-app:latest")
    .WithDockerfile("./Dockerfile");

// Or add a project that builds to a container
var webApp = builder.AddProject<Projects.WebApp>("webapp");

builder.Build().Run();
```

**Key capabilities:**
- **Dockerfile deployment** directly to Azure App Service
- **Container image building** as part of the deployment process
- **Unified deployment model** across different Azure compute environments
- **Seamless local-to-cloud experience** for containerized applications

This feature bridges the gap between container development and Azure App Service deployment, allowing developers to use the same container-based workflows they use locally in production Azure environments.

### 🏷️ Azure resource name exposure

.NET Aspire 9.4 now consistently exposes the actual names of deployed Azure resources through the `NameOutputReference` property. This enables applications to access the real Azure resource names that get generated during deployment, which is essential for scenarios requiring direct Azure resource coordination.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("appstorage");
var signalr = builder.AddAzureSignalR("notifications");

// Access the actual deployed Azure resource names
var api = builder.AddProject<Projects.Api>("api")
                .WithEnvironment("STORAGE_NAME", storage.Resource.NameOutputReference)
                .WithEnvironment("SIGNALR_NAME", signalr.Resource.NameOutputReference);

builder.Build().Run();
```

This is particularly valuable for:
- **External automation scripts** that need to interact with deployed Azure resources
- **Monitoring and alerting systems** that reference resources by their actual names
- **Cross-service coordination** where services need to know exact Azure resource identifiers
- **Infrastructure as Code scenarios** where generated names must be referenced elsewhere

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

### ⚡ Azure Functions Container Apps integration

.NET Aspire 9.4 improves Azure Functions deployment to Azure Container Apps by automatically setting the correct function app kind. This ensures Azure Functions are properly recognized and managed within the Azure Container Apps environment.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("functions-env");

// Azure Functions project deployed to Container Apps
var functionsApp = builder.AddAzureFunctionsProject<Projects.MyFunctions>("functions");

builder.Build().Run();
```

**Key improvements:**
- **Automatic function app kind setting** - Azure Functions are correctly identified as "functionapp" kind in Container Apps
- **Better Azure portal integration** - Functions appear correctly in the Azure portal with proper functionality
- **Improved runtime behavior** - Enhanced compatibility with Azure Container Apps runtime environment
- **Streamlined deployment** - Simplified deployment process with correct metadata

This change resolves issues where Azure Functions deployed to Container Apps weren't properly recognized by Azure tooling and monitoring systems, providing a more seamless serverless experience.

### 🎯 Enhanced emulator consistency

Azure emulator resources now include `EmulatorResourceAnnotation` for consistent tooling support across all emulator implementations, providing better development experience and tooling integration.

## 🔗 Integrations updates

### 🗄️ Database hosting improvements

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

### 🔐 Keycloak realm import simplification

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

### 💻 New `aspire exec` command

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

### 📊 Enhanced publish and deploy output

.NET Aspire 9.4 significantly improves the feedback and progress reporting during publish and deploy operations, providing clearer visibility into what's happening during deployment processes.

**Key improvements:**
- **Enhanced progress reporting** with detailed step-by-step feedback during publishing
- **Container runtime health checks** that provide clear status updates during container operations
- **Better error messaging** with more descriptive information when deployments fail
- **Improved publishing context** that tracks and reports on resource deployment status
- **Cleaner output formatting** that makes it easier to follow deployment progress

These improvements make it much easier to understand what's happening during `aspire deploy` and `aspire publish` operations, helping developers debug issues more effectively and gain confidence in their deployment processes.

The enhanced output is particularly valuable for:
- **CI/CD pipelines** where clear logging is essential for troubleshooting
- **Complex deployments** with multiple resources and dependencies
- **Container-based deployments** where build and push operations need clear status reporting
- **Team environments** where deployment logs need to be easily interpreted by different team members

## 📋 Project template improvements

.NET Aspire 9.4 introduces significant enhancements to project templates, including .NET 10 support and improved file naming conventions.

### 🚀 .NET 10 framework support

All .NET Aspire project templates now support .NET 10 with enhanced framework selection:

- **Multi-framework support**: Templates support .NET 8.0, .NET 9.0, and .NET 10.0
- **Smart defaults**: .NET 9.0 remains the default target framework
- **Version-specific templates**: Separate template configurations for different Aspire versions
- **Framework parameter**: Use `--framework net10.0` to target .NET 10 specifically

```bash
# Create a new Aspire project targeting .NET 10
dotnet new aspire --framework net10.0

# Create an app host project targeting .NET 10  
dotnet new aspire-apphost --framework net10.0
```

### 📝 Improved file naming convention

The `aspire-apphost` template now uses a more descriptive file naming convention:

- **AppHost.cs**: The main program file is now named `AppHost.cs` instead of `Program.cs`
- **Semantic clarity**: The filename clearly indicates this is an Aspire app host
- **Better organization**: Makes it easier to distinguish app host files in multi-project solutions

**Before (9.3 and earlier)**:
```
MyApp.AppHost/
├── Program.cs          ← Generic name
├── Aspire.AppHost1.csproj
└── appsettings.json
```

**After (9.4)**:
```
MyApp.AppHost/
├── AppHost.cs          ← Descriptive name
├── Aspire.AppHost1.csproj
└── appsettings.json
```

The content and functionality remain unchanged—only the filename has been updated to be more descriptive and semantically meaningful.

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
- **Version update notifications** to alert users when new CLI versions are available
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

### 🏗️ CLI infrastructure improvements

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

### 📦 Azure Storage API consolidation

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

### 🚫 Azure Container Apps hybrid mode removal

.NET Aspire 9.4 removes support for **hybrid mode Azure Container Apps** deployments, simplifying the infrastructure generation approach and removing legacy azd-based infrastructure patterns.

**What was removed**:
- **Hybrid infrastructure generation** where Azure Developer CLI (azd) was responsible for creating the Azure Container App Environment
- **`BicepSecretOutput` APIs** in Azure Container Apps logic, which were only used in hybrid mode scenarios
- **Legacy compatibility layer** between Aspire and azd for Container Apps infrastructure

```csharp
// ✅ Current approach (unified infrastructure generation):
var builder = DistributedApplication.CreateBuilder(args);

// Aspire handles all infrastructure generation
var containerEnv = builder.AddAzureContainerAppEnvironment("production");

// WithComputeEnvironment is only needed when multiple compute environments exist
var api = builder.AddProject<Projects.Api>("api");
// If only one compute environment exists, it's automatically used

// Example with multiple environments requiring disambiguation:
var stagingEnv = builder.AddAzureContainerAppEnvironment("staging");
var productionEnv = builder.AddAzureContainerAppEnvironment("production");

var stagingApi = builder.AddProject<Projects.Api>("staging-api")
    .WithComputeEnvironment(stagingEnv);  // Required for disambiguation

var productionApi = builder.AddProject<Projects.Api>("production-api")
    .WithComputeEnvironment(productionEnv);  // Required for disambiguation

builder.Build().Run();
```

**Migration impact**: 
- **`PublishAsAzureContainerApps()`** no longer provisions infrastructure - it only adds customization annotations
- **All infrastructure generation** is now handled consistently by Aspire.Hosting.Azure.AppContainers
- **Simplified deployment model** with cleaner separation between Aspire and azd responsibilities
- **Removed dependencies** on azd-specific infrastructure patterns

**Benefits of the change**:
- **Unified infrastructure approach** - All Azure Container Apps infrastructure is managed by Aspire
- **Simplified codebase** - Removal of dual-mode complexity and legacy compatibility code
- **Better consistency** - Container Apps infrastructure generation aligns with other Azure resources
- **Cleaner API surface** - Elimination of obsolete `BicepSecretOutput` patterns

This change affects applications that were relying on the hybrid mode where azd generated the container app environment. All Azure Container Apps infrastructure is now consistently managed through Aspire's infrastructure generation.

### 🔄 Azure Storage component registration updates

Client registration methods for Azure Storage have been standardized with new naming conventions:

```csharp
// ❌ Before (obsolete):
builder.AddAzureTableClient("tables");         // Obsolete
builder.AddKeyedAzureTableClient("tables");    // Obsolete
builder.AddAzureBlobClient("blobs");            // Obsolete
builder.AddKeyedAzureBlobClient("blobs");       // Obsolete
builder.AddAzureQueueClient("queues");          // Obsolete
builder.AddKeyedAzureQueueClient("queues");     // Obsolete

// ✅ After (recommended):
builder.AddAzureTableServiceClient("tables");         // Standardized naming
builder.AddKeyedAzureTableServiceClient("tables");    // Standardized naming
builder.AddAzureBlobServiceClient("blobs");           // Standardized naming
builder.AddKeyedAzureBlobServiceClient("blobs");      // Standardized naming
builder.AddAzureQueueServiceClient("queues");         // Standardized naming
builder.AddKeyedAzureQueueServiceClient("queues");    // Standardized naming
```

**Migration impact**: Update all client registration calls to use the new `*ServiceClient` naming convention.

### 🔑 Azure Key Vault secret reference changes

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

### 🗄️ Database initialization method changes

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

### 🔐 Keycloak realm import simplification

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

### 🔄 Resource lifecycle event updates

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

### ⚠️ Known parameter deprecations

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

### 🔧 ParameterResource.Value synchronous behavior change

The `ParameterResource.Value` property now blocks synchronously when waiting for parameter value resolution, which can potentially cause deadlocks in async contexts. The new `GetValueAsync()` method should be used instead for proper async handling.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Parameters that need resolution
var apiKey = builder.AddParameter("api-key", secret: true);
var connectionString = builder.AddParameter("connection-string", secret: true);

// ❌ Before (can cause deadlocks in async contexts):
builder.AddProject<Projects.Api>("api")
    .WithEnvironment("API_KEY", apiKey.Resource.Value)  // Blocks synchronously - can deadlock
    .WithEnvironment("CONNECTION_STRING", connectionString.Resource.Value);

// ✅ After (recommended for async contexts):
// Use the parameter resources directly with WithEnvironment - they handle async resolution internally
builder.AddProject<Projects.Api>("api")
    .WithEnvironment("API_KEY", apiKey)  // Let Aspire handle async resolution
    .WithEnvironment("CONNECTION_STRING", connectionString);

// Or if you need the actual value in custom code with WithEnvironment callback:
builder.AddProject<Projects.Api>("api")
    .WithEnvironment("API_KEY", async (context, cancellationToken) =>
    {
        return await apiKey.Resource.GetValueAsync(cancellationToken);  // Proper async handling
    })
    .WithEnvironment("CONNECTION_STRING", async (context, cancellationToken) =>
    {
        return await connectionString.Resource.GetValueAsync(cancellationToken);
    });

// For non-async contexts where blocking is acceptable:
var syncValue = apiKey.Resource.Value;  // Still works but may block
```

**Migration impact**: When working with `ParameterResource` values in async contexts, use the new `GetValueAsync()` method instead of the `Value` property to avoid potential deadlocks. For `WithEnvironment()` calls, prefer passing the parameter resource directly rather than accessing `.Value` synchronously.

### 🔧 Kafka configuration method changes

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
