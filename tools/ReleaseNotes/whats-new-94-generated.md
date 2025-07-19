---
title: What's new in .NET Aspire 9.4
description: Learn what's new in the official general availability release of .NET Aspire 9.4.
ms.date: 12/24/2024
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

To upgrade your existing .NET Aspire projects to 9.4, update the following package references in your App Host project:

```xml
<PackageReference Include="Aspire.AppHost" Version="9.4.0" />
<PackageReference Include="Aspire.Hosting.*" Version="9.4.0" />
```

For integration components in your service projects, update to version 9.4.0:

```xml
<PackageReference Include="Aspire.Azure.*" Version="9.4.0" />
<PackageReference Include="Aspire.Microsoft.*" Version="9.4.0" />
<PackageReference Include="Aspire.StackExchange.Redis" Version="9.4.0" />
<!-- Other Aspire components -->
```

## 🖥️ App model enhancements

### ✨ External services support

Before .NET Aspire 9.4, integrating with existing services outside your Aspire application required complex workarounds with manual connection string management and custom resource definitions. You had to handle service discovery and configuration injection manually.

This release introduces **external service resources**, enabling you to seamlessly model and reference any external service within your app model with full support for service discovery and dependency injection.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Reference an external API that your team doesn't control
var paymentApi = builder.AddExternalService("payment-api");

// Reference an existing database with connection details
var legacyDb = builder.AddExternalService("legacy-database");

// Your services automatically get the connection information
var orderService = builder.AddProject<Projects.OrderService>("orders")
    .WithReference(paymentApi)    // Gets payment API endpoint
    .WithReference(legacyDb);     // Gets database connection string

var inventoryService = builder.AddProject<Projects.InventoryService>("inventory")
    .WithReference(legacyDb);     // Multiple services can reference same external resource

builder.Build().Run();
```

This enables **hybrid architectures** where new Aspire applications can seamlessly connect to existing infrastructure, making migration and brownfield integration scenarios much simpler. Your services receive the external service endpoints through the same dependency injection mechanism as other Aspire resources.

**Enhanced external service integration:**

External services now support **comprehensive integration patterns** including environment variable injection and health checking:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// External service with parameter-driven URL
var paymentApiUrl = builder.AddParameter("payment-api-url");
var paymentApi = builder.AddExternalService("payment-api", paymentApiUrl)
    .WithHttpHealthCheck("/health", statusCode: 200);          // Health monitoring

// External service with direct URL
var legacyDb = builder.AddExternalService("legacy-db", "Server=legacy.db.com;Database=orders;");

// Services can reference external services in multiple ways
var orderService = builder.AddProject<Projects.OrderService>("orders")
    .WithReference(paymentApi)                                 // Service discovery reference
    .WithEnvironment("PAYMENT_API_URL", paymentApi)           // Environment variable injection
    .WithReference(legacyDb);

// External services can also be used in environment variable scenarios
var inventoryService = builder.AddProject<Projects.InventoryService>("inventory")
    .WithEnvironment("EXTERNAL_API_ENDPOINT", paymentApi)     // Direct environment injection
    .WithEnvironment("LEGACY_CONNECTION", legacyDb.Resource); // Resource value injection

builder.Build().Run();
```

**External service capabilities:**
- **Parameter integration**: External service URLs driven by parameters
- **Health checking**: HTTP health checks for external service monitoring
- **Environment variable injection**: Direct environment variable assignment from external services
- **Service discovery**: Full integration with Aspire's service discovery mechanisms
- **Multiple reference patterns**: Both `.WithReference()` and `.WithEnvironment()` support

### ✨ Enhanced YARP reverse proxy configuration

Managing reverse proxy scenarios in .NET Aspire previously required maintaining separate JSON configuration files and manually coordinating service endpoints, making it difficult to leverage Aspire's service discovery capabilities.

YARP integration has been **completely redesigned with a fluent configuration API** that eliminates JSON config files and provides seamless integration with Aspire's service discovery.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Define your backend services
var catalogApi = builder.AddProject<Projects.CatalogApi>("catalog");
var orderApi = builder.AddProject<Projects.OrderApi>("orders");
var userApi = builder.AddProject<Projects.UserApi>("users");

// Configure YARP gateway with fluent API - no JSON files needed
var gateway = builder.AddYarp("api-gateway")
    .WithConfiguration(yarp =>
    {
        // Route requests to different services based on path
        yarp.AddRoute("/api/catalog", catalogApi);
        yarp.AddRoute("/api/orders", orderApi);
        yarp.AddRoute("/api/users", userApi);
        
        // Default route catches everything else
        yarp.AddRoute("/", catalogApi);
    });

builder.Build().Run();
```

**Key benefits:**
- **No configuration files**: Routes are defined in code alongside your services
- **Automatic service discovery**: YARP automatically discovers your Aspire service endpoints
- **Type safety**: Compile-time checking prevents configuration errors
- **Dynamic updates**: Routes update automatically when services scale or move

This makes API gateway scenarios much simpler to implement and maintain, especially in development where service endpoints change frequently.

**Advanced YARP transform capabilities:**

Beyond basic routing, YARP now includes **comprehensive request/response transformation** through a fluent API:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ApiService>("api");

var gateway = builder.AddYarp("gateway")
    .WithConfiguration(yarp =>
    {
        // Route with comprehensive transformations
        yarp.AddRoute("/api/v1", apiService)
            .WithTransformPathPrefix("/api/v1")                    // Add path prefix
            .WithTransformRequestHeader("X-Gateway", "Aspire")     // Add request header
            .WithTransformResponseHeader("X-Processed-By", "YARP") // Add response header
            .WithTransformForwarded(useHost: true, useProto: true) // Forward headers
            .WithTransformUseOriginalHostHeader()                  // Preserve host
            .WithTransformQueryValue("version", "v1")              // Add query parameter
            .WithOrder(1);                                         // Route precedence

        // Advanced route with method and host matching
        yarp.AddRoute("/admin", apiService)
            .WithMatchMethods("GET", "POST")
            .WithMatchHosts("admin.example.com", "admin.local")
            .WithTransformPathSet("/internal/admin")               // Rewrite path
            .WithTransformRequestHeaderRemove("X-External")       // Remove headers
            .WithMaxRequestBodySize(1048576);                     // 1MB limit

        // Route with conditional response transforms
        yarp.AddRoute("/public", apiService)
            .WithTransformResponseHeaderRemove("X-Internal-Info", 
                ResponseCondition.Success)                         // Remove on success only
            .WithTransformClientCertHeader("X-Client-Cert");      // Forward client cert
    });

builder.Build().Run();
```

**Transform capabilities include:**
- **Path transforms**: Prefix addition/removal, pattern matching, complete path rewriting
- **Header transforms**: Request/response header manipulation with conditional logic
- **Query transforms**: Parameter addition, removal, and route value injection
- **Authentication transforms**: Client certificate forwarding and auth header management
- **Conditional logic**: Response-state-based transformations
- **Performance controls**: Request size limits and timeout configuration

This enables **enterprise-grade API gateway functionality** with comprehensive request/response manipulation capabilities previously requiring external tools or complex middleware.

### 🔧 Improved container file management

Previously, copying files into containers required verbose callback syntax that was difficult to understand and maintain for simple file copying scenarios, and provided limited options for permissions and ownership management.

Container resources now support **simplified file copying with enhanced control** that makes common scenarios much more straightforward:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Simple file copying - much cleaner than before
var webApp = builder.AddContainer("webapp", "nginx:alpine")
    .WithContainerFiles("/usr/share/nginx/html", "./wwwroot")
    .WithContainerFiles("/etc/nginx/conf.d", "./nginx-config")
    
    // Enhanced control over permissions and ownership
    .WithContainerFiles("/app/scripts", "./deployment-scripts", 
        defaultOwner: 1000,                    // Set file owner
        defaultGroup: 1000,                    // Set file group  
        umask: UnixFileMode.UserRead |         // Set file permissions
               UnixFileMode.UserWrite |
               UnixFileMode.UserExecute);

// Advanced scenarios still use callback approach for dynamic content
var dynamicApp = builder.AddContainer("dynamic", "alpine:latest")
    .WithContainerFiles("/data", async (context, ct) =>
    {
        // Generate files dynamically based on context
        return new[]
        {
            new ContainerFile 
            { 
                Name = "config.json", 
                Contents = JsonSerializer.Serialize(context.PublishingOptions),
                Uid = 1000, 
                Gid = 1000,
                Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite
            }
        };
    });

builder.Build().Run();
```

**Container file management improvements:**
- **Simplified API**: Direct path-to-path copying without complex callbacks
- **Permission control**: Configurable file ownership, group, and umask settings
- **Dynamic content**: Callback approach still available for generated content
- **Source path support**: Files can reference source paths for container layer optimization
- **Directory support**: Automatic handling of directory structures and permissions

This **simplifies the most common use case** of copying local directories into containers while maintaining the callback approach for dynamic scenarios and providing fine-grained control over file permissions and ownership.

### 🔧 Resource lifecycle and deployment improvements

Major improvements to resource management and deployment workflows:

- **Resource endpoints allocation** - Events now fire correctly once per logical resource
- **Health check improvements** - Container runtime health checks are now conditional based on resource requirements
- **Resource failure handling** - Proper `ResourceFailedException` handling prevents CLI hangs
- **Publishing activity progress** - Enhanced progress reporting during deployment operations
- **Interaction service refinements** - Better error handling for unsupported interaction scenarios

**Enhanced resource state management:**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgreSQL("userdb", password: builder.AddParameter("db-password"));
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(database);

var app = builder.Build();

// Access enhanced resource notification service
var notifications = app.ResourceNotifications;

// Check current resource state
if (notifications.TryGetCurrentState("userdb", out var resourceEvent))
{
    Console.WriteLine($"Database state: {resourceEvent.Snapshot.State}");
    
    // New 'Active' state for running resources
    if (resourceEvent.Snapshot.State == KnownResourceStates.Active)
    {
        Console.WriteLine("Database is actively running and ready");
    }
}

// Resources now support OTLP export annotations
database.Resource.Annotations.Add(new OtlpExporterAnnotation());

await app.RunAsync();
```

**Resource management improvements:**
- **Enhanced state tracking**: New `Active` state for running resources
- **Current state access**: `TryGetCurrentState()` method for immediate state checking
- **OTLP export support**: Resources can be annotated for telemetry export
- **Emulator annotations**: Standardized emulator resource marking
- **Compute resource identification**: `GetComputeResources()` extension for filtering

### ✨ Enhanced emulator support

Working with emulators during development often required manual detection and different configuration logic, making it difficult to write code that worked seamlessly across development and production environments.

.NET Aspire 9.4 introduces **better emulator detection and management** that simplifies development workflows:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// These resources automatically detect emulator vs. real service
var storage = builder.AddAzureStorage("storage");
var cosmos = builder.AddAzureCosmosDB("cosmos");
var redis = builder.AddRedis("cache");

// Your application code can adapt behavior based on emulator detection
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(storage)
    .WithReference(cosmos)
    .WithReference(redis);

builder.Build().Run();
```

**Benefits for development:**
- **Automatic detection**: Resources automatically identify when running against emulators
- **Consistent APIs**: Same code works with both emulators and production services
- **Development optimization**: Emulator-specific optimizations happen automatically
- **Simplified testing**: No need for environment-specific configuration logic

This eliminates the need for complex environment detection logic in your application code, allowing you to focus on business logic while Aspire handles the infrastructure differences.

**Enhanced emulator detection API:**

Beyond automatic detection, .NET Aspire 9.4 provides **programmatic emulator detection** for custom logic and conditional configuration:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage");
var cosmos = builder.AddAzureCosmosDB("cosmos"); 
var redis = builder.AddRedis("cache");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(storage)
    .WithReference(cosmos)
    .WithReference(redis);

var app = builder.Build();

// Programmatically check if resources are running against emulators
bool storageIsEmulator = storage.Resource.IsEmulator();
bool cosmosIsEmulator = cosmos.Resource.IsEmulator();
bool redisIsEmulator = redis.Resource.IsEmulator();

// Configure application behavior based on emulator detection
if (storageIsEmulator)
{
    Console.WriteLine("Using Azure Storage Emulator - development mode active");
    // Enable additional debugging features
}

if (cosmosIsEmulator) 
{
    Console.WriteLine("Using Cosmos DB Emulator - bypassing certain production validations");
    // Skip expensive operations not needed in development
}

await app.RunAsync();
```

**Emulator detection benefits:**
- **Automatic discovery**: Resources automatically detect emulator vs. production environments
- **Programmatic access**: `IsEmulator()` extension method for custom logic
- **Consistent behavior**: Same detection logic across Azure Storage, Cosmos DB, Redis, and other services
- **Development optimization**: Enable/disable features based on emulator usage

## 🖥️ CLI enhancements

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

### 🔧 Enhanced deployment capabilities

The CLI's deployment infrastructure has been improved:

- **Deploy command** is now available behind feature flags for early adopters
- **Context-sensitive completion messages** provide better feedback during operations
- **.NET SDK availability checks** ensure compatibility before command execution  
- **Improved error handling** prevents CLI hangs when dashboard startup fails

### 🔧 Enhanced interaction capabilities

The CLI now supports **interactive mode** through integration with VS Code extensions and improved user interaction patterns:

- **Extension backchannel** communication for VS Code integration
- **Interactive prompts** with validation and guidance
- **Enhanced localization** with comprehensive translation support
- **Improved error handling** with actionable error messages

## 🖥️ Dashboard user experience improvements

### ✨ Enhanced interaction system

The Dashboard introduces a **comprehensive interaction system** that enables richer user experiences during development and deployment workflows:

- **Server-side validation** for interaction inputs ensures data quality
- **Custom input rendering** with support for parameter descriptions  
- **Message box and confirmation dialogs** with improved dismiss handling
- **Dashboard telemetry integration** for better insights into usage patterns

### 🔧 Improved resource visualization

Resource management and debugging capabilities have been enhanced:

- **Hidden resource toggle** - Console logs now update automatically when showing/hiding resources
- **Enhanced peer visualization** - Better support for uninstrumented resources including parameters, connection strings, and GitHub models
- **Improved resource matching** - Fixed logic to handle multiple resource matches correctly
- **Console log wrapping** - New option to wrap long log lines for better readability

### 🔧 Performance and reliability improvements

Several under-the-hood improvements enhance Dashboard stability:

- **Connection string parsing** improvements for better database resource handling
- **Proxied endpoints** support for improved connectivity scenarios  
- **Thread safety fixes** in Razor views to prevent concurrency issues
- **Service client reorganization** with cleaner separation of concerns

## ☁️ Azure integration updates

### ✨ Enhanced Azure Container Apps

Azure Container Apps integration has been enhanced with new configuration options and Dashboard support.

Azure Container Apps integration in previous versions lacked comprehensive monitoring setup and required manual configuration of logging and observability infrastructure, making it difficult to achieve production-ready deployments.

.NET Aspire 9.4 enhances Azure Container Apps with **integrated monitoring and dashboard capabilities**:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Create Log Analytics workspace for centralized logging
var logAnalytics = builder.AddAzureLogAnalyticsWorkspace("monitoring");

// Container Apps environment with enhanced monitoring
var containerEnv = builder.AddAzureContainerAppsEnvironment("app-env")
    .WithAzureLogAnalyticsWorkspace(logAnalytics)        // Integrate logging
    .WithDashboard(enable: true)                         // Enable built-in dashboard
    .WithAzdResourceNaming();                            // Consistent naming

// Deploy applications to the enhanced environment
var api = builder.AddProject<Projects.Api>("api")
    .PublishAsAzureContainerApp();

var worker = builder.AddProject<Projects.Worker>("worker") 
    .PublishAsAzureContainerApp();

builder.Build().Run();
```

**Azure Container Apps enhancements:**
- **Integrated Log Analytics**: Automatic workspace configuration for centralized logging
- **Dashboard integration**: Built-in monitoring dashboard with metrics and logs
- **Resource naming**: Consistent Azure resource naming through `WithAzdResourceNaming()`
- **Enhanced observability**: Automatic OTLP endpoint configuration for telemetry collection
- **Compute environment support**: Full `IAzureComputeEnvironmentResource` implementation

This provides **production-ready Azure Container Apps deployments** with comprehensive monitoring, logging, and observability configured automatically.

### 🔧 Azure resource improvements

Several Azure resources now expose **name output references** for better integration:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Azure resources with name output references
var storage = builder.AddAzureStorage("appstorage");
var signalr = builder.AddAzureSignalR("notifications");
var webpubsub = builder.AddAzureWebPubSub("realtime");

// Use name output references for consistent resource naming
var api = builder.AddProject<Projects.Api>("api")
    .WithEnvironment("STORAGE_ACCOUNT_NAME", storage.Resource.NameOutputReference)
    .WithEnvironment("SIGNALR_NAME", signalr.Resource.NameOutputReference)
    .WithEnvironment("WEBPUBSUB_NAME", webpubsub.Resource.NameOutputReference);

// This ensures your applications can reliably reference Azure resources
// by their actual deployed names, improving coordination between services
var monitoring = builder.AddProject<Projects.Monitoring>("monitoring")
    .WithEnvironment("AZURE_RESOURCES", JsonSerializer.Serialize(new
    {
        Storage = storage.Resource.NameOutputReference,
        SignalR = signalr.Resource.NameOutputReference,
        WebPubSub = webpubsub.Resource.NameOutputReference
    }));
```

- **Azure SignalR** - `NameOutputReference` property for resource naming
- **Azure Storage** - Enhanced with `NameOutputReference` and improved blob/queue management  
- **Azure WebPubSub** - `NameOutputReference` for consistent resource referencing

### ✨ Enhanced Azure Storage

Managing Azure Storage resources previously required separate steps to create blob containers and queues, often leading to complex initialization logic and potential race conditions during application startup.

Azure Storage has been **streamlined with direct container and queue creation** that improves reliability and simplifies resource management:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("appstorage");

// Create blob containers directly - no separate steps needed
var documentsContainer = storage.AddBlobContainer("documents");
var imagesContainer = storage.AddBlobContainer("images");

// Create queues directly on the storage resource
var processingQueue = storage.AddQueue("processing");
var notificationQueue = storage.AddQueue("notifications");

// Services get scoped access to specific containers/queues
var documentService = builder.AddProject<Projects.DocumentService>("docs")
    .WithReference(documentsContainer)   // Only accesses documents container
    .WithReference(processingQueue);     // Only accesses processing queue

var imageService = builder.AddProject<Projects.ImageService>("images")
    .WithReference(imagesContainer)      // Only accesses images container
    .WithReference(notificationQueue);   // Only accesses notification queue

builder.Build().Run();
```

**Security and reliability benefits:**
- **Least privilege access**: Services only get credentials for the specific containers/queues they need
- **Automatic creation**: Containers and queues are created automatically during deployment
- **Improved startup**: Health checks ensure containers exist before services start
- **Better isolation**: Different services can't accidentally access each other's storage

This approach provides better security isolation compared to giving services full storage account access, and eliminates common startup race conditions.

### ✨ Resource lifecycle events

Managing resource dependencies and coordinating startup sequences in previous versions required complex custom logic and polling mechanisms to determine when resources were ready or when endpoints became available.

.NET Aspire 9.4 introduces a **comprehensive resource event system** that provides precise lifecycle hooks for resource management and coordination:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgreSQL("userdb", password: builder.AddParameter("db-password"))
    // Execute logic when resource is initialized
    .OnInitializeResource(async (resource, evt, ct) =>
    {
        // Custom initialization logic
        Console.WriteLine($"Initializing {resource.Name}");
    })
    // Execute logic when endpoints are allocated
    .OnResourceEndpointsAllocated(async (resource, evt, ct) =>
    {
        Console.WriteLine($"Database available at: {evt.Services}");
    })
    // Execute logic before resource starts
    .OnBeforeResourceStarted(async (resource, evt, ct) =>
    {
        // Pre-startup validation or configuration
        await ValidateDatabaseConfiguration(resource);
    })
    // Execute logic when resource is fully ready
    .OnResourceReady(async (resource, evt, ct) =>
    {
        // Post-startup actions like schema migration
        await RunDatabaseMigrations(resource);
    });

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(database)
    // React when connection string becomes available
    .OnConnectionStringAvailable(async (resource, evt, ct) =>
    {
        Console.WriteLine($"Connection string ready: {evt.ConnectionString}");
    });

builder.Build().Run();
```

**Key lifecycle events available:**
- **InitializeResourceEvent**: Fired during resource initialization 
- **ResourceEndpointsAllocatedEvent**: Fired when endpoints are allocated
- **BeforeResourceStartedEvent**: Fired before resource startup
- **ResourceReadyEvent**: Fired when resource is fully operational
- **ConnectionStringAvailableEvent**: Fired when connection strings are ready

This enables **precise coordination** of complex startup sequences, dependency validation, and resource preparation without polling or custom state management.

### ✨ Interactive user input system

Collecting user input during application startup or deployment previously required external tools, environment variables, or complex custom prompting mechanisms that didn't integrate well with the Aspire tooling experience.

.NET Aspire 9.4 introduces an **experimental interactive input system** that enables rich user interaction directly within Aspire applications and tooling:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Enhanced parameters with descriptions and custom input
var connectionString = builder.AddParameter("database-connection", secret: true)
    .WithDescription("Database connection string for the production environment", enableMarkdown: true)
    .WithCustomInput(param => new InteractionInput
    {
        Label = "Production Database",
        Placeholder = "Server=...;Database=...;",
        InputType = InputType.SecretText,
        Required = true,
        Description = "Enter the **production** database connection string"
    });

var apiKey = builder.AddParameter("api-key", secret: true)
    .WithCustomInput(param => new InteractionInput
    {
        Label = "External API Key", 
        InputType = InputType.SecretText,
        MaxLength = 64,
        Required = true
    });

// Use the interaction service for dynamic prompts
var app = builder.Build();

// Access interaction service for runtime prompts
var interactionService = app.Services.GetRequiredService<IInteractionService>();

if (interactionService.IsAvailable)
{
    // Prompt for confirmation
    var deployConfirm = await interactionService.PromptConfirmationAsync(
        "Deploy to Production", 
        "Are you sure you want to deploy to the production environment?",
        new MessageBoxInteractionOptions { Intent = MessageIntent.Warning });
    
    if (deployConfirm.Canceled || !deployConfirm.Data)
    {
        return;
    }

    // Prompt for multiple inputs
    var inputs = await interactionService.PromptInputsAsync(
        "Deployment Configuration",
        "Configure deployment settings",
        new[]
        {
            new InteractionInput { Label = "Environment", InputType = InputType.Choice, 
                Options = new[] { KeyValuePair.Create("prod", "Production"), 
                                KeyValuePair.Create("staging", "Staging") } },
            new InteractionInput { Label = "Replicas", InputType = InputType.Number, Value = "3" }
        });
}

await app.RunAsync();
```

**Key interaction capabilities:**
- **Rich parameter input**: Custom input types, validation, and descriptions
- **Dynamic prompts**: Runtime confirmation, input, and message dialogs  
- **Multiple input types**: Text, secret text, choice, boolean, and number inputs
- **Validation support**: Custom validation with error messaging
- **Markdown support**: Rich text formatting in descriptions and messages

This provides a **consistent, integrated experience** for collecting user input across development, deployment, and operational scenarios.

### ✨ Resource command execution

Executing commands against running resources in previous versions required external tools, direct container access, or custom implementation of command interfaces that weren't integrated with the Aspire resource model.

.NET Aspire 9.4 introduces a **resource command system** that enables standardized command execution against any resource with full integration into the Dashboard and CLI:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddPostgreSQL("userdb", password: builder.AddParameter("db-password"));

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(database);

var app = builder.Build();

// Access the resource command service
var commandService = app.ResourceCommands;

// Execute commands against resources
await commandService.ExecuteCommandAsync("userdb", "backup-database");
await commandService.ExecuteCommandAsync("api", "clear-cache");

// Commands can also be executed by resource reference
await commandService.ExecuteCommandAsync(database.Resource, "restart");

await app.RunAsync();
```

**Command execution features:**
- **Standardized interface**: Consistent command execution across all resource types
- **Dashboard integration**: Commands appear as buttons in the Aspire Dashboard
- **CLI integration**: Commands available through `aspire exec` and other CLI tools
- **Parameter passing**: Commands can accept parameters and return results
- **State management**: Commands can be enabled/disabled based on resource state

This enables **standardized operational capabilities** across all resource types, making it easy to expose maintenance, debugging, and administrative functions directly through Aspire tooling.

### ✨ Enhanced publishing and deployment infrastructure  

Publishing and deployment workflows in previous versions lacked detailed progress reporting, standardized error handling, and extensible activity tracking, making it difficult to understand deployment status and diagnose issues during complex publishing operations.

.NET Aspire 9.4 introduces an **experimental enhanced publishing infrastructure** with rich progress reporting and standardized deployment workflows:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");

// Add deployment callbacks for custom logic
api.OnDeployingResource(async (resource, deployingContext, ct) =>
{
    var reporter = deployingContext.ActivityReporter;
    
    // Create deployment steps with progress tracking
    using var buildStep = await reporter.CreateStepAsync("Building container image");
    using var buildTask = await buildStep.CreateTaskAsync("Compiling application");
    
    await buildTask.UpdateAsync("Running dotnet publish...");
    // Custom build logic here
    await buildTask.SucceedAsync("Application compiled successfully");
    
    await buildStep.SucceedAsync("Container image built");
    
    // Create deployment step
    using var deployStep = await reporter.CreateStepAsync("Deploying to target environment");
    using var deployTask = await deployStep.CreateTaskAsync("Uploading artifacts");
    
    await deployTask.UpdateAsync("Uploading to container registry...");
    // Custom deployment logic
    await deployTask.SucceedAsync("Artifacts uploaded");
    
    await deployStep.SucceedAsync("Deployment completed");
});

builder.Build().Run();
```

**Enhanced container building options:**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Api>("api");

// Access container image builder for advanced scenarios  
var containerBuilder = builder.Services.GetRequiredService<IResourceContainerImageBuilder>();

// Build with specific options
await containerBuilder.BuildImageAsync(api.Resource, new ContainerBuildOptions
{
    ImageFormat = ContainerImageFormat.Oci,
    TargetPlatform = ContainerTargetPlatform.LinuxAmd64,
    OutputPath = "./container-images"
});

// Build multiple images efficiently
await containerBuilder.BuildImagesAsync(new[] { api.Resource }, new ContainerBuildOptions
{
    TargetPlatform = ContainerTargetPlatform.LinuxArm64
});
```

**Key publishing improvements:**
- **Step-based progress**: Hierarchical progress reporting with steps and tasks
- **Rich status updates**: Detailed status messages with completion states
- **Container build options**: Configurable image formats and target platforms  
- **Deployment callbacks**: Custom logic integration at deployment time
- **Activity reporting**: Comprehensive progress tracking and error reporting

This provides **professional-grade deployment experiences** with clear progress indication, detailed error reporting, and extensible customization points for complex deployment scenarios.

## 🔧 Integrations updates

### ✨ Enhanced parameter configuration

Parameter configuration in previous versions was limited to basic value prompting without rich descriptions, custom input types, or integrated validation, making it difficult to create user-friendly configuration experiences.

.NET Aspire 9.4 enhances the parameter system with **rich descriptions and custom input configuration**:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Parameters with rich descriptions and custom inputs
var connectionString = builder.AddParameter("database-url", secret: true)
    .WithDescription("""
        ## Database Connection String
        
        Enter the connection string for your **production database**.
        
        **Format**: `Server=myServerAddress;Database=myDataBase;Uid=myUsername;Pwd=myPassword;`
        
        > **Note**: This will be stored securely and not displayed in logs.
        """, enableMarkdown: true)
    .WithCustomInput(param => new InteractionInput
    {
        Label = "Production Database Connection",
        Placeholder = "Server=...;Database=...;User Id=...;Password=...;",
        InputType = InputType.SecretText,
        Required = true,
        MaxLength = 500,
        Description = "Secure connection string for the production database"
    });

var environment = builder.AddParameter("target-environment")
    .WithDescription("Select the target deployment environment")
    .WithCustomInput(param => new InteractionInput
    {
        Label = "Deployment Environment",
        InputType = InputType.Choice,
        Required = true,
        Options = new[]
        {
            KeyValuePair.Create("dev", "Development"),
            KeyValuePair.Create("staging", "Staging Environment"),
            KeyValuePair.Create("prod", "Production")
        }
    });

var replicas = builder.AddParameter("replica-count")
    .WithDescription("Number of service replicas to deploy")
    .WithCustomInput(param => new InteractionInput
    {
        Label = "Service Replicas",
        InputType = InputType.Number,
        Value = "3",
        Required = true
    });

// Use parameters in resource configuration
var database = builder.AddExternalService("production-db", connectionString);
var api = builder.AddProject<Projects.Api>("api")
    .WithReference(database)
    .WithEnvironment("TARGET_ENV", environment)
    .WithEnvironment("REPLICA_COUNT", replicas);

builder.Build().Run();
```

**Parameter configuration enhancements:**
- **Markdown descriptions**: Rich text descriptions with formatting support
- **Custom input types**: Text, secret text, choice, boolean, and number inputs
- **Input validation**: Required fields, maximum length, and custom validation
- **Choice options**: Predefined value lists with display names
- **Placeholder text**: Helpful input guidance for users
- **Public value access**: Direct `GetValueAsync()` method on parameter resources

This enables **professional parameter collection experiences** with rich descriptions, appropriate input types, and comprehensive validation.

### ✨ Updated project templates

.NET Aspire 9.4 includes **updated project templates** with modern tooling and dependency versions:

- **App Host file structure** - Templates now use `AppHost.cs` instead of `Program.cs` for better clarity
- **Updated test dependencies** - XUnit updated to v3.1.0, MSTest and NUnit templates refreshed
- **Enhanced test coverage** - Integration test templates include `Aspire.Hosting.Testing` package
- **Improved localization** - Templates support additional languages and better internationalization

### ✨ Database hosting improvements

Database initialization in previous versions required understanding different initialization patterns for each database provider, leading to inconsistent APIs and complex setup code.

Several database integrations now provide **unified initialization patterns** that work consistently across all providers:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Consistent initialization API across all database providers
var postgres = builder.AddPostgreSQL("userdb", password: builder.AddParameter("db-password"))
    .WithInitFiles("./postgres-init");  // Initialize with SQL scripts

var mysql = builder.AddMySql("productdb", password: builder.AddParameter("mysql-password"))
    .WithInitFiles("./mysql-init");     // Same pattern for MySQL

var mongo = builder.AddMongoDB("sessiondb")
    .WithInitFiles("./mongo-init");     // Same pattern for MongoDB

// Your services connect the same way regardless of database type
var userService = builder.AddProject<Projects.UserService>("users")
    .WithReference(postgres);

var productService = builder.AddProject<Projects.ProductService>("products")
    .WithReference(mysql);

var sessionService = builder.AddProject<Projects.SessionService>("sessions")
    .WithReference(mongo);

builder.Build().Run();
```

**Developer experience improvements:**
- **Unified API**: Same `WithInitFiles()` method across PostgreSQL, MySQL, MongoDB, and Oracle
- **Simplified setup**: No need to learn provider-specific initialization methods
- **Better error handling**: Consistent error messages and validation across providers
- **Automatic mounting**: Files are automatically mounted and executed during container startup

This eliminates the learning curve of different database-specific initialization APIs and provides a consistent developer experience regardless of which database you choose.

### 🔧 Container orchestration enhancements

Previously, Docker Compose services ran separately from your Aspire application, making it difficult to monitor and manage them alongside your other resources during development.

**Docker Compose** integration has been enhanced with **Dashboard support** that provides unified monitoring:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Docker Compose services now appear in Aspire Dashboard
var infrastructure = builder.AddDockerCompose("infrastructure", "docker-compose.yml")
    .WithDashboard();  // Enable Dashboard integration

// You can also customize the dashboard configuration
var composeEnv = builder.AddDockerComposeEnvironment("external-services")
    .WithDashboard(dashboard => 
    {
        dashboard.WithHostPort(18888);  // Custom dashboard port
    });

builder.Build().Run();
```

**Unified development experience:**
- **Single dashboard**: All your resources (Aspire + Compose) in one place
- **Health monitoring**: Docker Compose service health appears alongside Aspire resources
- **Log aggregation**: Stream logs from all services through the same interface
- **Consistent tooling**: Same debugging and monitoring tools for all resources

This eliminates the need to switch between different tools and dashboards during development, providing a single pane of glass for your entire application stack.

**Enhanced Docker Compose configuration:**

Beyond dashboard integration, Docker Compose support has been enhanced with **advanced configuration management**:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Enhanced Docker Compose environment with custom dashboard
var infrastructure = builder.AddDockerComposeEnvironment("infrastructure")
    .WithDashboard(dashboard => dashboard.WithHostPort(18888))  // Custom port
    .ConfigureComposeFile(compose =>
    {
        // Add configurations programmatically
        compose.AddConfig(new Config
        {
            Name = "app-config",
            Content = JsonSerializer.Serialize(new { Environment = "Development" })
        });
        
        // Add services with enhanced configuration
        compose.AddService(new Service
        {
            Name = "redis",
            Image = "redis:alpine"
        }.AddConfig(new ServiceNodes.ConfigReference
        {
            Source = "app-config",
            Target = "/usr/local/etc/redis/redis.conf",
            Mode = UnixFileMode.UserRead | UnixFileMode.GroupRead
        }));
    });

// Traditional file-based approach with dashboard
var external = builder.AddDockerCompose("external-services", "docker-compose.yml")
    .WithDashboard(enabled: true);

builder.Build().Run();
```

**Docker Compose API improvements:**
- **Programmatic configuration**: Add configs, services, and networks through code
- **Enhanced config support**: Inline content and external file configurations
- **Improved permission handling**: Type-safe Unix file mode settings
- **Service composition**: Fluent API for building complex service configurations
- **Custom dashboard**: Configurable dashboard ports and settings

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
```

**What changed**: The `isReadOnly` parameter was removed as a default parameter and is now only available when explicitly needed, making the common case simpler while still allowing advanced scenarios.

## 💔 Breaking changes

### 🔧 Azure Storage API simplification (Breaking change)

The previous Azure Storage API required multiple steps to create blob containers and queues, leading to verbose code and potential confusion about resource relationships.

This release **simplifies the API by providing direct container and queue creation** methods on the storage resource itself.

#### ✅ New behavior in this release

- Direct container creation on storage resources
- Direct queue creation on storage resources  
- Simplified method signatures with sensible defaults

#### ⚠️ Breaking change

The intermediate blob service step has been removed to streamline the API.

**Before:**
```csharp
var storage = builder.AddAzureStorage("storage");
var blobService = storage.AddBlobs("blobs");          // Extra step required
var container = blobService.AddBlobContainer("documents");
```

**After:**
```csharp
var storage = builder.AddAzureStorage("storage");
var container = storage.AddBlobContainer("documents");  // Direct creation
```

**Migration:**
1. Remove the intermediate `AddBlobs()` call
2. Call `AddBlobContainer()` directly on the storage resource
3. Update any references to the blob service to use the storage resource instead

### 🔧 Database initialization API unification (Breaking change)

Database initialization methods were inconsistent across providers, with complex parameter signatures that were difficult to understand and use correctly.

This release **standardizes database initialization** with a simplified, consistent API across all database providers.

#### ✅ New behavior in this release

- Unified `WithInitFiles()` method across all database providers
- Simplified parameters with automatic read-only mounting
- Consistent error handling and validation

#### ⚠️ Breaking change

The `WithInitBindMount` method with complex parameters has been replaced with the simpler `WithInitFiles` method.

**Before:**
```csharp
var mongo = builder.AddMongoDB("mongo")
    .WithInitBindMount("./init", isReadOnly: true);  // Complex parameters

var mysql = builder.AddMySql("mysql")  
    .WithInitBindMount("./mysql-scripts", isReadOnly: false);
```

**After:**
```csharp
var mongo = builder.AddMongoDB("mongo")
    .WithInitFiles("./init");  // Simplified, consistent API

var mysql = builder.AddMySql("mysql")
    .WithInitFiles("./mysql-scripts");  // Same pattern across providers
```

**Migration:**
1. Replace `WithInitBindMount()` calls with `WithInitFiles()`
2. Remove the `isReadOnly` parameter (handled automatically)
3. Use the same pattern across PostgreSQL, MySQL, MongoDB, and Oracle

### 🔧 Keycloak realm import simplification (Breaking change)

The Keycloak `WithRealmImport` method included a confusing `isReadOnly` parameter that was rarely needed and made the common case unnecessarily complex.

This release **simplifies the API by removing the confusing parameter** while maintaining advanced capabilities when needed.

#### ✅ New behavior in this release

- Default `WithRealmImport(string import)` method for common scenarios
- Optional overload with `isReadOnly` parameter for advanced cases
- Cleaner, more intuitive API for typical usage

#### ⚠️ Breaking change

The `isReadOnly` parameter is no longer required and has been removed from the default overload.

**Before:**
```csharp
var keycloak = builder.AddKeycloak("keycloak")
    .WithRealmImport("./realm.json", isReadOnly: false);  // Required parameter
```

**After:**
```csharp
var keycloak = builder.AddKeycloak("keycloak")
    .WithRealmImport("./realm.json");  // Simplified - parameter optional
```

**Migration:**
1. Remove the `isReadOnly` parameter from `WithRealmImport()` calls for typical usage
2. Use the two-parameter overload only when you need explicit control over read-only behavior

### YARP extension class renamed

The YARP extensions class has been **renamed for consistency with other Aspire resource extensions**:

```csharp
// Old class name (still works but deprecated):
// YarpServiceExtensions

// ✅ New class name:
// YarpResourceExtensions

// Usage remains the same:
var yarp = builder.AddYarp("gateway")
    .WithReference(backendApi)
    .WithTransforms(transforms => transforms
        .AddRequestHeader("X-Gateway", "Aspire")
        .AddPathPrefix("/api/v1"));
```

**Migration impact**: Update any direct references to `YarpServiceExtensions` to use `YarpResourceExtensions`. Most usage through the fluent API is unaffected.

Update your using statements if you were directly referencing the extension class.

## 🎯 Upgrade today

.NET Aspire 9.4 delivers significant improvements in developer experience, deployment capabilities, and Azure integrations. The enhanced CLI, improved Dashboard interactions, and streamlined Azure services make it easier than ever to build and deploy cloud-native applications.

To get started with .NET Aspire 9.4:

1. **Install or update** the latest [.NET Aspire tools](../fundamentals/setup-tooling.md)
2. **Update your projects** to reference version 9.4.0 packages
3. **Explore new features** like the interactive CLI and enhanced Dashboard
4. **Share your feedback** on [GitHub](https://github.com/dotnet/aspire) to help us improve

For detailed upgrade guidance and migration assistance, see the [.NET Aspire upgrade guide](../migration/upgrade.md).
