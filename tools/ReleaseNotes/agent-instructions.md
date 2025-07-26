# .NET Aspire Release Notes Generation Instructions

This document outlines the process for generating professional .NET Aspire release notes, from data collection through final documentation.

## 🎯 **Goal: Create `whats-new-{version}.md`**

The primary objective is to generate a comprehensive `whats-new-{version}.md` document that summarizes all major features, API changes, CLI enhancements, and breaking changes for the target release version.

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
   ./extract-api-changes-parallel.sh
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

## 🚨 CRITICAL ANALYSIS AND ACCURACY REQUIREMENTS

**All steps in this process must strictly follow these rules:**

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

### 3. **PRIMARY API SOURCE: THE UBER FILE**

The **UBER API FILE** is the single source of truth for all API references:
```
analysis-output/api-changes-build-current/all-api-changes.txt
```

This file contains:
- **Complete API files** with method signatures for all components
- **Actual API definitions** from the current build
- **All components**: Azure services, hosting platforms, data services
- **Exact method signatures** including parameter names and types

### Mandatory Workflow for Documentation

1. **📋 ANALYZE ALL COMPONENT FILES**: Review every `analysis-output/*.md` file for commit-based changes
2. **📊 CHECK API CHANGES SUMMARY**: Use `api-changes-build-current/api-changes-summary.md` for new API discoveries
3. **🔍 VERIFY IN UBER FILE**: Before writing ANY code sample, search the uber file for exact API definitions
4. **✅ USE ONLY CONFIRMED APIS**: If it's not in the uber file, it doesn't exist
5. **💻 PROVIDE SAMPLES FOR API CHANGES**: Every API change mentioned should include a code sample
6. **⚡ INCLUDE CLI COMMANDS**: Every CLI change should show the actual command syntax
7. **❌ NEVER INVENT APIS**: No made-up methods, parameters, or fluent chains

### Component Coverage in Uber File

The uber file includes verified APIs for:
- **Azure Services**: AIFoundry, AppConfiguration, AppContainers, ApplicationInsights, AppService, CognitiveServices, CosmosDB, EventHubs, KeyVault, OperationalInsights, PostgreSQL, Redis, Search, ServiceBus, SignalR, Sql, Storage, WebPubSub
- **Hosting Platforms**: Docker, Kubernetes, GitHub.Models
- **Data Services**: MongoDB, MySql, Oracle, PostgreSQL  
- **Infrastructure**: Azure (base), Hosting (base), Yarp
- **Client Components**: Azure.Data.Tables, Azure.Storage.Blobs, Azure.Storage.Queues, Microsoft.Extensions.Configuration.AzureAppConfiguration

## 📝 Writing Accurate Documentation

### Example Workflow: Azure AI Foundry

**Step 1: Search Uber File**
```bash
grep -A 10 -B 2 "AddAzureAIFoundry" analysis-output/api-changes-build-current/all-api-changes.txt
```

**Step 2: Find Actual API**
```csharp
// From uber file: 
public static ApplicationModel.IResourceBuilder<Azure.AzureAIFoundryResource> AddAzureAIFoundry(this IDistributedApplicationBuilder builder, string name)
public static ApplicationModel.IResourceBuilder<Azure.AzureAIFoundryDeploymentResource> AddDeployment(this ApplicationModel.IResourceBuilder<Azure.AzureAIFoundryResource> builder, string name, string modelName, string modelVersion, string format)
```

**Step 3: Create Accurate Example**
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

### CLI Changes Source

For CLI-related content, use the commit analysis from:
```
analysis-output/Aspire.Cli.md
```

**Verified CLI changes from commits:**
- `aspire exec` command (feature-flagged, preview)
- `aspire deploy` command (feature-flagged, preview)  
- `aspire config` with dot notation support
- Enhanced `aspire new`, `aspire add`, `aspire run`, `aspire publish`
- Health column added to `aspire run`
- Localization support added

**❌ DO NOT document CLI commands not found in commit analysis**

### Validation Process

Before publishing any release notes:
1. **Cross-reference all code samples** with the uber file
2. **Verify CLI commands** against Aspire.Cli.md commit analysis
3. **Check breaking changes** against actual API diffs
4. **Search for APIs** before writing any example:
   ```bash
   grep -n "MethodName" analysis-output/api-changes-build-current/all-api-changes.txt
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
   - Format code elements in backticks: `IDistributedApplicationBuilder`, `AddRedis()`
   - Organize by impact: breaking changes, major features, enhancements
   - Include emojis consistently per template (🖥️, ✨, 🔗, 🚀, etc.)

## Success Criteria

A successful `whats-new-{version}.md` document is achieved when:
- ✅ ALL component analysis files have been reviewed for major features
- ✅ ALL commits in component files have been analyzed for comprehensive coverage
- ✅ API changes summary has been used to identify new APIs and changes
- ✅ All code samples use APIs verified in uber file
- ✅ All CLI commands exist in commit analysis and include actual command syntax
- ✅ All API references are accurate and complete with working code samples
- ✅ Breaking changes reflect actual API diffs
- ✅ No fictional features are documented
- ✅ Document follows the established template structure from `data/whats-new-*.md` files

## Remember: **Comprehensive Analysis + Accuracy over completeness**

Better to have fewer, accurate examples than many incorrect ones. Analyze ALL component files and commits for complete coverage, then use the uber file as your source of truth for all API samples. Every API change needs a working code sample, every CLI change needs the actual command.

**End Goal**: A professional, accurate, and comprehensive `whats-new-{version}.md` document that developers can trust and use effectively.
