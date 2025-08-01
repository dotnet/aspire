# Aspire Release Notes Generation Instructions

This document outlines the process for generating professional Aspire release notes, from data collection through final documentation.

## 🎯 **Goal: Create `whats-new-{version}.md`**

The primary objective is to generate a comprehensive `whats-new-{version}.md` document that summarizes all major features, API changes, CLI enhancements, and breaking changes for the target release version, and links to relevant documentation where appropriate.

## 📥 Data Collection Steps

1. **Analyze Component Changes**

   ```bash
   ./analyze-all-components.sh <base_branch> <target_branch>
   ```

   This generates:
   - Individual component analysis files (`analysis-output/*.md`)
   - A summary of all component changes (`analysis-output/analysis-summary.md`)
   - Each file contains:
     - Overall change statistics
     - Complete commit history
     - Top contributors
     - Categorized commits (features/bugs/breaking changes)

2. **Extract API Changes**

   ```bash
   ./extract-api-changes.sh
   ```

   This generates:
   - **Uber API File**: `analysis-output/api-changes-build-current/all-api-changes.txt` (comprehensive API definitions)
   - API change summary (`analysis-output/api-changes-build-current/api-changes-summary.md`)
   - Detailed API diffs (`analysis-output/api-changes-build-current/api-changes-diff.txt`)

3. **Generate What's New Document**
   - **GOAL**: Create `whats-new-{version}.md` for the target release
   - **Use templates in `data/` directory** as structure and formatting guide (e.g., `data/whats-new-93.md`)
   - **Analyze ALL files** in `analysis-output/` directory for comprehensive coverage
   - **Review ALL commits** in each component analysis file to identify major features
   - **Use API changes summary** from `analysis-output/api-changes-build-current/api-changes-summary.md`
   - Focus on developer impact with accurate code samples for API changes
   - Include CLI commands for CLI-related changes

## 🔍 COMMIT ANALYSIS AND FEATURE EXTRACTION

### Understanding Component Analysis Files

Each component analysis file (`analysis-output/*.md`) contains:

- **Change Summary**: File counts and statistics
- **Complete Commit List**: All commits for that component between releases
- **Top Contributors**: Who made the changes
- **Categorized Commits**: Features, bug fixes, breaking changes

### How to Analyze Commits for Features

#### Step 1: Read Commit Messages for Context

```markdown
# Example from analysis file:
6b5ef9919 [release/9.4] Add ContainerBuildOptions support to ResourceContainerImageBuilder for customizing dotnet publish (#10312)
853c91898 Rename IPublishingActivityProgressReporter to IPublishingActivityReporter (#10253)
c5e604f4b Add dashboard resource to AddDockerComposeEnvironment (#9597)
4ee28c24b Only expose endpoint port in docker compose if external is set to true (#9604)
```

#### Step 2: Look Up GitHub Issues and Pull Requests for Additional Context

**CRITICAL**: When commit messages include GitHub issue references (e.g., `#10587`, `(#10648)`), **always look up the issue/PR for deeper understanding**:

- **Use GitHub API tools** to fetch PR/issue details: `mcp_github_get_pull_request` or `mcp_github_get_issue`
- **Extract PR descriptions** to understand the problem being solved and implementation details
- **Review customer impact** statements and use cases from the issue
- **Identify related issues** if the commit references a backport or duplicate
- **Examine PR body for screenshots, examples, and detailed explanations**
- **Incorporate GitHub context** into feature descriptions for better developer understanding
- **Include GitHub links** in the final documentation for traceability

#### Example: Enhancing commit understanding with GitHub issues

```markdown
# Commit message:
9ccd68154 Add links between telemetry and resources in grid values (#10648)

# After looking up GitHub PR #10648:
- Problem: Difficulty navigating between telemetry data and resources in dashboard
- Solution: Custom components for clickable resource names, span IDs, and trace IDs
- Screenshots: Shows before/after navigation experience
- Customer impact: Reduces time to correlate telemetry across distributed applications
- Implementation: Property grid value components with navigation links

# Final documentation includes GitHub reference:
### ✨ Enhanced telemetry navigation
The dashboard now provides better navigation between telemetry data and resources ([#10648](https://github.com/dotnet/aspire/pull/10648)).
```

#### GitHub Issue Pattern Recognition

**Look for these patterns in commit messages:**
- `(#12345)` - Pull request reference
- `Fixes #12345` - Closes an issue  
- `Closes #12345` - Closes an issue
- `[release/X.Y] ... (#12345)` - Backport with PR reference
- Multiple references: `(#10170) (#10313) (#10316)` - Related PRs/issues

**For each GitHub reference found:**
1. **Fetch the PR/issue details** using GitHub API tools
2. **Read the description** for implementation context
3. **Extract user-facing benefits** from problem statements
4. **Note any breaking changes** mentioned in PR descriptions
5. **Use screenshots/examples** from PRs to enhance documentation
6. **Add GitHub links** to the final What's New document

#### Step 3: Identify Feature Types by Commit Message Patterns

- **"Add"** commits → New features or APIs
- **"Rename"** commits → Breaking changes or API updates
- **"Improve/Enhance"** commits → Enhancements to existing features
- **"Fix"** commits → Bug fixes (usually not featured unless significant)
- **"Support for"** commits → New platform/technology integrations

#### Step 3: Extract User-Facing Impact

For each significant commit, determine:

- **What capability does this enable?** (new feature)
- **What does this change for developers?** (API impact)
- **What problem does this solve?** (use case)
- **Is this breaking?** (migration needed)

### Commit-to-Feature Translation Process

#### Example: Docker Compose Commits Analysis

From `Aspire.Hosting.Docker.md`:

```markdown
c5e604f4b Add dashboard resource to AddDockerComposeEnvironment (#9597)
4ee28c24b Only expose endpoint port in docker compose if external is set to true (#9604)
```

**Translation Process:**

1. **Commit c5e604f4b**: "Add dashboard resource to AddDockerComposeEnvironment"
   - **Feature**: Docker Compose with integrated Aspire Dashboard
   - **User Impact**: Developers can now add dashboard integration to Docker Compose environments
   - **API Change**: New `.WithDashboard()` method on `AddDockerComposeEnvironment`

2. **Commit 4ee28c24b**: "Only expose endpoint port in docker compose if external is set to true"
   - **Feature**: Enhanced Docker Compose security
   - **User Impact**: Better security by selective port exposure
   - **Behavior Change**: Internal endpoints use `expose`, external endpoints use `ports`

### Multi-Component Feature Identification

**Look for Related Commits Across Components:**

Example: Azure Storage API consolidation appears in multiple files:

- `Aspire.Hosting.Azure.Storage.md`: API changes
- `Aspire.Azure.Data.Tables.md`: Client registration changes
- `Aspire.Azure.Storage.Blobs.md`: Service client updates

**Synthesis Process:**

1. **Identify the pattern**: Multiple storage components have similar "rename" commits
2. **Find the theme**: API consolidation and standardization
3. **Create unified feature**: "Azure Storage API consolidation" section
4. **Show migration path**: Before/after examples for all affected components

## 🎯 COMPREHENSIVE ANALYSIS APPROACH

### 1. **ANALYZE ALL COMPONENT FILES**

Review every file in `analysis-output/` directory:

- **Individual component files** (`*.md`) contain complete commit histories
- **Look at ALL commits** in each file to identify major features and changes
- **Categorize changes** by impact: breaking changes, major features, enhancements
- **Identify patterns** across multiple components for broader themes

### 2. **API CHANGES DISCOVERY**

Use `analysis-output/api-changes-build-current/api-changes-summary.md` as the primary source for:

- **New API additions** across all components
- **Breaking changes** and deprecations
- **Method signature changes** and parameter updates
- **New extension methods** and builder patterns

### 4. **GIT COMMIT ANALYSIS FOR MISSING FEATURES**

Sometimes important features may not be immediately obvious from component analysis files. Use direct git analysis:

```bash
# Find commits with specific keywords
git log --oneline --grep="Add.*support" --since="2024-01-01"
git log --oneline --grep="container.*support" --since="2024-01-01"

# Analyze specific commits mentioned by stakeholders
git show --stat <commit-hash>
git show <commit-hash> --name-only
```

**Example: Stakeholder-Identified Commits**
When given specific commit hashes to analyze:

```bash
git show --stat bdd1d34c6  # Azure App Service container support
git show --stat d4eacc676  # Enhanced publish/deploy output
git show --stat 4ee28c24b  # Docker Compose security improvements
git show --stat 039c42594  # Azure Functions Container Apps integration
```

**Process for Stakeholder Commits:**

1. **Get commit details**: Use `git show --stat` to understand scope
2. **Look up GitHub issues**: If commit message references an issue (e.g., `#10587`), use GitHub API tools to get additional context
3. **Identify user impact**: What new capability or improvement does this enable? (Enhanced by issue context)
4. **Find related files**: Use `git show --name-only` to see what changed
5. **Create feature section**: Write user-facing documentation based on the changes and issue context
6. **Verify APIs**: Cross-reference any new APIs with the uber file

### 5. **COMMIT PRIORITIZATION FOR RELEASE NOTES**

**High Priority Commits (Must Include):**

- New resource types (`Add.*Resource`)
- New integration support (`Add.*support`, `Support for.*`)
- Breaking API changes (`Rename.*`, `Remove.*`, `Change.*`)
- Major CLI features (`Add.*command`, `.*exec.*`, `.*deploy.*`)
- Security improvements (`security`, `expose.*port`)

**Medium Priority Commits (Consider Including):**

- Performance improvements (`Improve.*performance`, `Optimize.*`)
- Enhanced configuration (`Add.*configuration`, `Support.*options`)
- Better error handling (`Improve.*error`, `Add.*validation`)
- Developer experience (`Enhance.*`, `Better.*`)

**Low Priority Commits (Usually Skip):**

- Bug fixes (`Fix.*`) unless critical or user-visible
- Internal refactoring without user impact
- Documentation updates
- Test improvements
- Code cleanup

### 6. **PRIMARY API SOURCE: THE UBER FILE**

The **UBER API FILE** is the single source of truth for all API references:

```text
analysis-output/api-changes-build-current/all-api-changes.txt
```

This file contains:

- **Complete API files** with method signatures for all components
- **Actual API definitions** from the current build
- **All components**: Azure services, hosting platforms, data services
- **Exact method signatures** including parameter names and types

### Mandatory Workflow for Documentation

1. **📋 ANALYZE ALL COMPONENT FILES**: Review every `analysis-output/*.md` file for commit-based changes
   - Read through ALL commits in each component file
   - Look for patterns: "Add", "Rename", "Support", "Improve"
   - Extract user-facing impact from commit messages
   - Group related commits across components into unified features

2. **🔍 EXTRACT FEATURES FROM COMMITS**: Transform commits into user-facing documentation
   - Identify new capabilities enabled by each commit
   - Determine API changes and their impact on developers
   - Create feature sections based on commit analysis
   - Synthesize related commits into cohesive feature stories

3. **📊 CHECK API CHANGES SUMMARY**: Use `api-changes-build-current/api-changes-summary.md` for new API discoveries

4. **🔬 ANALYZE STAKEHOLDER COMMITS**: When specific commits are mentioned, deep dive with git commands
   - Use `git show --stat <commit>` to understand scope
   - Use `git show <commit> --name-only` to see affected files
   - Extract user-facing improvements from commit changes

5. **🔍 VERIFY IN UBER FILE**: Before writing ANY code sample, search the uber file for exact API definitions

6. **✅ USE ONLY CONFIRMED APIS**: If it's not in the uber file, it doesn't exist

7. **💻 PROVIDE SAMPLES FOR API CHANGES**: Every API change mentioned should include a code sample

8. **⚡ INCLUDE CLI COMMANDS**: Every CLI change should show the actual command syntax

9. **❌ NEVER INVENT APIS**: No made-up methods, parameters, or fluent chains

### Component Coverage in Uber File

The uber file includes verified APIs for:

- **Azure Services**: AIFoundry, AppConfiguration, AppContainers, ApplicationInsights, AppService, CognitiveServices, CosmosDB, EventHubs, KeyVault, OperationalInsights, PostgreSQL, Redis, Search, ServiceBus, SignalR, Sql, Storage, WebPubSub
- **Hosting Platforms**: Docker, Kubernetes, GitHub.Models
- **Data Services**: MongoDB, MySql, Oracle, PostgreSQL
- **Infrastructure**: Azure (base), Hosting (base), Yarp
- **Client Components**: Azure.Data.Tables, Azure.Storage.Blobs, Azure.Storage.Queues, Microsoft.Extensions.Configuration.AzureAppConfiguration

## 📝 Writing Features from Commit Analysis

### From Commits to Documentation Sections

**Step-by-Step Process:**

1. **Identify the Commit Pattern**
   - Scan commit messages for keywords: "Add", "Support", "Improve", "Rename"
   - Look for technology names: "Azure", "Docker", "Kubernetes", "Redis"
   - Find capability words: "authentication", "configuration", "deployment"

2. **Extract User Value**
   - **What problem does this solve?** (the "why")
   - **What can developers now do?** (the capability)
   - **How do they use it?** (the API/CLI)

3. **Find Related Documentation**
- Using the MicrosoftDocs MCP, search for existing documentation about the feature, starting with Aspire documentation.
- Documentation can be referenced in multiple ways. If the doc is part of the aspire docset (learn.microsoft.com/dotnet/aspire/*) you can use a relative path (assume What's New is one level - ie ../ - below "root" dotnet/aspire/)
- If the docset is on learn.microsoft.com, but not from Aspire, you can use an xref in the link path - for example, something under Azure docs (learn.microsoft.com/azure/ai-foundry/overview) would be [Azure AI Foundry documentation](xref:/azure/ai-foundry/overview)
- If a new API is explicitly called out, use an xref to the API docs via the fully qualified API namespace For example, `Aspire.Hosting.ApplicationModel.BeforeStartEvent` or simply `BeforeStartEvent` becomes <xref:aspire.hosting.applicationmodel.beforestartevent> 
- Do NOT put links to docs or APIs in the sample code. Only put them in the brief description above to set context, or the explanation for follow up content and API docs.
- Some conceptual docs may not exist yet. If you are unsure of if a link's content fits, or if something is relevant, do not add a link at all.

1. **Create Feature Section Structure**

   ```markdown
   ### ✨ [Feature Name]
   
   [Brief description of the new capability and its value]
   
   ```csharp
   // Code example using verified APIs from uber file
   ```
   
   [Explanation of the example, key benefits, and where to learn more]

### Real Examples of Commit-to-Feature Translation

**Commit:** `c5e604f4b Add dashboard resource to AddDockerComposeEnvironment (#9597)`

**Feature Section:**

```markdown
### 🖥️ Docker Compose with Aspire Dashboard integration

You can now integrate the Aspire Dashboard directly into your Docker Compose environments, providing a unified monitoring experience for both containerized and Aspire-managed resources.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose")
    .WithDashboard()  // ✨ New integration capability
    .WithComposeFile("docker-compose.yml");

await builder.Build().RunAsync();
```

This enhancement allows you to monitor Docker Compose services alongside your Aspire resources in a [single dashboard interface](../fundamentals/dashboard/overview.

**Commit:** `bdd1d34c6 Add support for containers with Dockerfile to AzureAppServiceEnvironmentResource`

**Feature Section:**

```markdown
### 🐳 Azure App Service container deployment with Dockerfile support

Azure App Service environments now support deploying containerized applications directly with <xref:Aspire.Hosting.ContainerResourceBuilderExtensions.WithDockerfile*>, making it easier to deploy custom container images to Azure.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var appService = builder.AddAzureAppService("myapp")
    .WithDockerfile("./src/MyApp");  // ✨ New Dockerfile support

await builder.Build().RunAsync();
```

This feature streamlines container deployment to Azure App Service by building and deploying directly from your Dockerfile without requiring pre-built container images.

### Grouping Related Commits into Unified Features

When multiple commits contribute to a single user-facing capability:

**Multiple Related Commits:**
- `853c91898 Rename IPublishingActivityProgressReporter to IPublishingActivityReporter`
- `d4eacc676 Improve the publish/deploy output with better progress reporting`
- `a1b2c3d4e Add structured logging to publish operations`

**Unified Feature Section:**

```markdown
### 🚀 Enhanced publish and deploy experience

The [publish and deploy experience](../deployment/overview) has been significantly improved with better progress reporting, clearer output formatting, and enhanced logging.

```bash
# Enhanced output with structured progress
aspire publish --interactive
✅ Building project...
✅ Publishing container images...
✅ Deploying to Azure...
📊 Deployment completed successfully in 2m 15s
```

Key improvements include:

- **Structured progress reporting**: Clear status updates throughout the deployment process
- **Better error messages**: More actionable error information when deployments fail
- **Enhanced logging**: Detailed logs for troubleshooting deployment issues

## 📝 Writing Accurate Documentation

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

### Do Not Invent APIs

```csharp
// ❌ WRONG: These methods are NOT in the uber file
builder.AddAzureAIFoundry("ai")
    .WithModel("gpt-4", modelId: "gpt-4-1106")      // INVENTED
    .WithEndpoint("chat", deployment: "chat-latest") // INVENTED
    .WithRoleAssignments(roles: KeyVaultBuiltInRole.SecretsUser) // INVENTED
```

### Builder Context Matters

**IDistributedApplicationBuilder vs IHostApplicationBuilder**: Pay attention to which builder interface the extension methods target:

- **`IDistributedApplicationBuilder`** - Extension methods for app model resources and hosting

  ```csharp
  var builder = DistributedApplication.CreateBuilder(args);  // Returns IDistributedApplicationBuilder
  builder.AddAzureStorage("storage");     // ✅ Extension method on IDistributedApplicationBuilder
  builder.AddProject<Projects.Api>("api"); // ✅ Extension method on IDistributedApplicationBuilder
  ```

- **`IHostApplicationBuilder`** - Extension methods for service registration

  ```csharp
  var builder = WebApplication.CreateBuilder(args);  // Returns WebApplicationBuilder : IHostApplicationBuilder
  builder.AddAzureTableServiceClient("tables");     // ✅ Extension method on IHostApplicationBuilder
  builder.AddKeyedAzureBlobServiceClient("blobs");  // ✅ Extension method on IHostApplicationBuilder
  ```

**❌ Common Mistake**: Using service registration extension methods on `IDistributedApplicationBuilder`:

```csharp
// ❌ WRONG: AddAzureTableServiceClient is an extension method on IHostApplicationBuilder registration, not app hosting
var builder = DistributedApplication.CreateBuilder(args);
builder.AddAzureTableServiceClient("tables");  // Does not exist on IDistributedApplicationBuilder
```

### CLI Changes Source

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

#### ❌ DO NOT document CLI commands not found in commit analysis

### Validation Process

Before publishing any release notes:

1. **Cross-reference all code samples** with the uber file
2. **Verify CLI commands** against Aspire.Cli.md commit analysis
3. **Check breaking changes** against actual API diffs
4. **Run markdownlint on the generated document** while ignoring line length violations:

   ```bash
   npx markdownlint-cli@0.45.0 data/whats-new-{version}.md --disable MD013
   ```

5. **Search for APIs** before writing any example:

   ```bash
   grep -n "MethodName" analysis-output/api-changes-build-current/all-api-changes.txt
   ```

6. **Store traceability references** for each documented change:
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

### Template Structure Requirements

Follow the established structure from `data/whats-new-*.md` files:

1. **Frontmatter** (YAML header with title, description, ms.date)
2. **Main Title** (`# What's new in .NET Aspire {version}`)
3. **Introduction** with supported .NET versions and feedback links
4. **Version Support Policy** information
5. **Major Sections** with emoji headers (🖥️, ✨, 🔗, 🚀, etc.)
6. **Code Examples** with proper syntax highlighting
7. **Breaking Changes** section (if applicable)
8. **Migration Guidance** for breaking changes

**Example Template Structure:**

```markdown
---
title: What's new in .NET Aspire {version}
description: Learn what's new in .NET Aspire {version}.
ms.date: {date}
---

# What's new in .NET Aspire {version}

📢 .NET Aspire {version} is the next [major|minor] version release...

## 🖥️ App model enhancements
### ✨ Feature Name
[Description and code samples]

## 🚀 CLI improvements
[CLI commands and examples]
```

### Core Documentation Rules

1. **Comprehensive Analysis**
   - Review ALL component analysis files in `analysis-output/`
   - Examine ALL commits in each file to understand the full scope of changes
   - Use `api-changes-summary.md` to identify new API additions and changes
   - Summarize major features and themes across components

2. **API Changes with Samples**
   - Every API change must include a code sample
   - Search uber file before writing ANY code sample
   - Match parameter names and types exactly as shown
   - Show migration steps for breaking changes found in diffs
   - Never invent or assume APIs exist

3. **CLI Changes with Commands**
   - Every CLI change must show the actual command syntax
   - Use commit analysis from `Aspire.Cli.md` for verified CLI changes
   - Include examples of command usage and output where applicable
   - Document any new flags, options, or command behaviors

4. **Style and Structure**
   - **Follow templates in `data/` directory** (e.g., `data/whats-new-91.md`, `data/whats-new-92.md`, `data/whats-new-93.md`)
   - **Maintain consistent document structure**: frontmatter, intro, major sections with emojis
   - Use active voice and developer-focused language
   - Format code elements in backticks: `IDistributedApplicationBuilder`, `AddRedis()`.
   - If you reference an API for the first time link, or it is a new API, link to its docs with an inline xref like <xref:Fully.Qualified.Api.Namespace>
   - Organize by impact: breaking changes, major features, enhancements
   - Include emojis consistently per template (🖥️, ✨, 🔗, 🚀, etc.)

## Success Criteria

A successful `whats-new-{version}.md` document is achieved when:

- ✅ ALL component analysis files have been reviewed for commit-based features
- ✅ ALL significant commits have been analyzed and translated into user-facing features
- ✅ Commit patterns have been identified and grouped into cohesive feature sections
- ✅ Multi-component features have been synthesized from related commits across components
- ✅ Stakeholder-identified commits have been analyzed and documented appropriately
- ✅ API changes summary has been used to identify new APIs and changes
- ✅ All code samples use APIs verified in uber file
- ✅ All CLI commands exist in commit analysis and include actual command syntax
- ✅ All API references are accurate and complete with working code samples
- ✅ Breaking changes reflect actual API diffs and commits
- ✅ No fictional features are documented
- ✅ Document follows the established template structure from `data/whats-new-*.md` files
- ✅ The reader knows where to go for more information about the features

## Remember: **Comprehensive Commit Analysis + Accuracy over completeness**

The foundation of great release notes is thorough commit analysis. Every significant commit should be considered for inclusion, translated into developer-facing language, and verified with accurate API samples. Better to have fewer, accurate features that represent real commit-based improvements than many speculative ones.

**Process Summary**:

1. **Analyze commits** → Identify patterns and capabilities
2. **Extract user value** → What problems do these commits solve?
3. **Group related commits** → Create unified feature stories
4. **Verify APIs** → Use uber file for all code samples
5. **Write developer-focused** → Clear examples and migration paths

**End Goal**: A professional, accurate, and comprehensive `whats-new-{version}.md` document that reflects the actual improvements made through commits, with verified APIs that developers can trust and use effectively.
