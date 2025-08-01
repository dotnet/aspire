# API Documentation and Code Samples

## 🔑 **PRIMARY API SOURCE: THE UBER FILE**

The **UBER API FILE** is the single source of truth for all API references:

```text
analysis-output/api-changes-build-current/all-api-changes.txt
```

This file contains:

- **Complete API files** with method signatures for all components
- **Actual API definitions** from the current build
- **All components**: Azure services, hosting platforms, data services
- **Exact method signatures** including parameter names and types

### Component Coverage in Uber File

The uber file includes verified APIs for:

- **Azure Services**: AIFoundry, AppConfiguration, AppContainers, ApplicationInsights, AppService, CognitiveServices, CosmosDB, EventHubs, KeyVault, OperationalInsights, PostgreSQL, Redis, Search, ServiceBus, SignalR, Sql, Storage, WebPubSub
- **Hosting Platforms**: Docker, Kubernetes, GitHub.Models
- **Data Services**: MongoDB, MySql, Oracle, PostgreSQL
- **Infrastructure**: Azure (base), Hosting (base), Yarp
- **Client Components**: Azure.Data.Tables, Azure.Storage.Blobs, Azure.Storage.Queues, Microsoft.Extensions.Configuration.AzureAppConfiguration

## Writing Accurate Documentation

### Example Workflow: Azure AI Foundry

#### Step 1: Search Uber File

```bash
grep -A 10 -B 2 "AddAzureAIFoundry" analysis-output/api-changes-build-current/all-api-changes.txt
```

#### Step 2: Find Actual API

```csharp
// From uber file: 
public static ApplicationModel.IResourceBuilder<Azure.AzureAIFoundryResource> AddAzureAIFoundry(this IDistributedApplicationBuilder builder, string name)
public static ApplicationModel.IResourceBuilder<Azure.AzureAIFoundryDeploymentResource> AddDeployment(this ApplicationModel.IResourceBuilder<Azure.AzureAIFoundryResource> builder, string name, string modelName, string modelVersion, string format)
```

#### Step 3: Create Accurate Example

```csharp
// ✅ CORRECT: Based on actual APIs from uber file
var aiFoundry = builder.AddAzureAIFoundry("ai-foundry");
var deployment = aiFoundry.AddDeployment("gpt-4", "gpt-4", "1106", "OpenAI");
```

## Do Not Invent APIs

```csharp
// ❌ WRONG: These methods are NOT in the uber file
builder.AddAzureAIFoundry("ai")
    .WithModel("gpt-4", modelId: "gpt-4-1106")      // INVENTED
    .WithEndpoint("chat", deployment: "chat-latest") // INVENTED
    .WithRoleAssignments(roles: KeyVaultBuiltInRole.SecretsUser) // INVENTED
```

## Builder Context Matters

**IDistributedApplicationBuilder vs IHostApplicationBuilder**: Pay attention to which builder interface the extension methods target:

### **`IDistributedApplicationBuilder`** - Extension methods for app model resources and hosting

```csharp
var builder = DistributedApplication.CreateBuilder(args);  // Returns IDistributedApplicationBuilder
builder.AddAzureStorage("storage");     // ✅ Extension method on IDistributedApplicationBuilder
builder.AddProject<Projects.Api>("api"); // ✅ Extension method on IDistributedApplicationBuilder
```

### **`IHostApplicationBuilder`** - Extension methods for service registration

```csharp
var builder = WebApplication.CreateBuilder(args);  // Returns WebApplicationBuilder : IHostApplicationBuilder
builder.AddAzureTableServiceClient("tables");     // ✅ Extension method on IHostApplicationBuilder
builder.AddKeyedAzureBlobServiceClient("blobs");  // ✅ Extension method on IHostApplicationBuilder
```

### **❌ Common Mistake**: Using service registration extension methods on `IDistributedApplicationBuilder`:

```csharp
// ❌ WRONG: AddAzureTableServiceClient is an extension method on IHostApplicationBuilder registration, not app hosting
var builder = DistributedApplication.CreateBuilder(args);
builder.AddAzureTableServiceClient("tables");  // Does not exist on IDistributedApplicationBuilder
```

## CLI Changes Source

For CLI-related content, use the commit analysis from:

```text
analysis-output/Aspire.Cli.md
```

**Verified CLI changes from commits:**

- `aspire exec` command (feature-flagged, preview)
- `aspire deploy` command (feature-flagged, preview)
- `aspire config` with dot notation support
- Enhanced `aspire new`, `aspire add`, `aspire run`, `aspire publish`
- Health column added to `aspire run`
- Localization support added

### ❌ DO NOT document CLI commands not found in commit analysis

## Documentation Linking Guidelines

### Find Related Documentation

- Using the MicrosoftDocs MCP, search for existing documentation about the feature, starting with Aspire documentation.
- Documentation can be referenced in multiple ways. If the doc is part of the aspire docset (learn.microsoft.com/dotnet/aspire/*) you can use a relative path (assume What's New is one level - ie ../ - below "root" dotnet/aspire/)
- If the docset is on learn.microsoft.com, but not from Aspire, you can use an xref in the link path - for example, something under Azure docs (learn.microsoft.com/azure/ai-foundry/overview) would be [Azure AI Foundry documentation](xref:/azure/ai-foundry/overview)
- If a new API is explicitly called out, use an xref to the API docs via the fully qualified API namespace. For example, `Aspire.Hosting.ApplicationModel.BeforeStartEvent` or simply `BeforeStartEvent` becomes <xref:aspire.hosting.applicationmodel.beforestartevent>
- Do NOT put links to docs or APIs in the sample code. Only put them in the brief description above to set context, or the explanation for follow up content and API docs.
- Some conceptual docs may not exist yet. If you are unsure of if a link's content fits, or if something is relevant, do not add a link at all.

## Validation Process

Before writing any API samples:

1. **Search for APIs** before writing any example:

   ```bash
   grep -n "MethodName" analysis-output/api-changes-build-current/all-api-changes.txt
   ```

2. **Cross-reference all code samples** with the uber file
3. **Verify CLI commands** against Aspire.Cli.md commit analysis
4. **Check breaking changes** against actual API diffs

## API Documentation Rules

### Core Rules

1. **Every API change must include a code sample**
2. **Search uber file before writing ANY code sample**
3. **Match parameter names and types exactly as shown**
4. **Show migration steps for breaking changes found in diffs**
5. **Never invent or assume APIs exist**

### CLI Documentation Rules

1. **Every CLI change must show the actual command syntax**
2. **Use commit analysis from `Aspire.Cli.md` for verified CLI changes**
3. **Include examples of command usage and output where applicable**
4. **Document any new flags, options, or command behaviors**

### Accuracy Standards

- **✅ USE ONLY CONFIRMED APIS**: If it's not in the uber file, it doesn't exist
- **💻 PROVIDE SAMPLES FOR API CHANGES**: Every API change mentioned should include a code sample
- **⚡ INCLUDE CLI COMMANDS**: Every CLI change should show the actual command syntax
- **❌ NEVER INVENT APIS**: No made-up methods, parameters, or fluent chains

## Traceability References

Store traceability references for each documented change:

- **Commit SHA or message** from component analysis
- **GitHub Issue ID** (if referenced in commit message)
- **GitHub Pull Request number** (if available)
- **Component name** where the change was found

This enables verification and backtracking to actual source changes. Example format:

```
Feature: Dashboard telemetry navigation improvements
Source: commit "Add telemetry peer navigation" in Aspire.Dashboard
GitHub PR: #10648
GitHub Issue: #10645 (if referenced)
```
