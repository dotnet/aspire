# Aspire MCP Skills-as-Resources

## Overview

This specification describes the design and implementation of a skills-as-resources pattern for the Aspire MCP server. Skills are dynamic, discoverable instruction files exposed as MCP resources, decoupling skill content from Aspire CLI releases.

## Background

### Motivation

Currently, MCP prompts (like `aspire-pair-programmer`, `troubleshoot-app`, etc.) are implemented as static C# classes with hardcoded instruction text. This approach has limitations:

1. **Release coupling** - Updating skill content requires a new Aspire CLI release
2. **No discoverability** - Clients cannot browse available skills without invoking prompts
3. **No progressive disclosure** - All prompt content is returned at once, no lazy loading
4. **Limited extensibility** - Cannot add custom skills without modifying CLI source

### Prior Art

- **FastMCP SkillsProvider** - Python implementation exposing SKILL.md files as MCP resources with URI scheme `skill://{name}/SKILL.md`
- **Claude Code skills** - Local skills stored in `~/.claude/skills/` directories
- **Aspire repo skills** - Existing `.github/skills/` directory with SKILL.md files for development guidance

### MCP Resource Pattern

The MCP protocol supports resources as addressable content units:

```
resources/list    - List available resources
resources/read    - Retrieve resource content by URI
```

Resources can be:
- **Static resources** - Fixed URIs like `skill://add-integration`
- **Resource templates** - Parameterized URIs like `skill://{name}` (RFC 6570)

## Design Goals

### Primary Objectives

1. **Dynamic skills** - Skill content fetched from remote sources (aspire.dev) and updated independently of CLI releases
2. **Backward compatible prompts** - Existing prompt endpoints continue working, using skills as backing content
3. **Progressive disclosure** - Clients can list skills (lightweight) before reading full content
4. **Skill manifest** - Each skill exposes metadata (name, description, version)
5. **Extensibility** - Future support for local custom skills

### Non-Goals

- **File system skills** - Not implementing local `~/.aspire/skills/` directories (future consideration)
- **Skill versioning** - Not tracking skill version history (may add later)
- **Skill editing** - MCP server is read-only for skills

## Architecture

### Component Overview

```txt
┌─────────────────────────────────────────────────────────────────────┐
│                         MCP Server                                  │
├─────────────────────────────────────────────────────────────────────┤
│  Resources (NEW)                   │  Tools                         │
│  ├─ skill://aspire-pair-programmer │  ├─ list_docs                  │
│  ├─ skill://troubleshoot-app       │  ├─ search_docs                │
│  ├─ skill://debug-resource         │  ├─ get_doc                    │
│  ├─ skill://add-integration        │  └─ ...                        │
│  └─ skill://deploy-app             │                                │
│                                    │  Prompts (backward compat)     │
│                                    │  ├─ aspire-pair-programmer     │
│                                    │  ├─ troubleshoot-app           │
│                                    │  └─ ...                        │
├─────────────────────────────────────────────────────────────────────┤
│  Skills Services                                                    │
│  ├─ ISkillsIndexService  - Discover and index skills from sources  │
│  ├─ ISkillsProvider      - Unified access to skill content         │
│  └─ AspireDevSkillsSource - Fetch skills from aspire.dev           │
└─────────────────────────────────────────────────────────────────────┘
```

### Data Flow Diagram

```mermaid
sequenceDiagram
    participant Client as MCP Client
    participant Server as MCP Server
    participant Skills as SkillsIndexService
    participant Docs as DocsIndexService
    participant Web as aspire.dev

    Note over Client,Web: List Skills Flow
    Client->>Server: resources/list
    Server->>Skills: ListSkillsAsync()
    Skills-->>Server: List<SkillInfo>
    Server-->>Client: Resource[] (URIs + descriptions)

    Note over Client,Web: Read Skill Flow
    Client->>Server: resources/read (skill://add-integration)
    Server->>Skills: GetSkillAsync("add-integration")

    alt Skill not cached
        Skills->>Docs: EnsureIndexedAsync()
        Skills->>Docs: GetDocumentAsync("add-integration")
        Docs-->>Skills: LlmsDocument
        Skills->>Skills: BuildSkillContent()
    end

    Skills-->>Server: SkillContent
    Server-->>Client: TextResourceContents
```

### Skill URI Scheme

Skills are exposed via a `skill://` URI scheme:

```
skill://aspire-pair-programmer        - Main persona skill
skill://troubleshoot-app              - Troubleshooting workflow
skill://debug-resource                - Resource debugging workflow
skill://add-integration               - Integration guidance
skill://add-integration?type=redis    - Parameterized skill (future)
skill://deploy-app                    - Deployment workflow
```

### Service Interfaces

#### ISkillsProvider

Provides unified access to skills from multiple sources:

```csharp
internal interface ISkillsProvider
{
    /// <summary>
    /// Lists all available skills from all registered sources.
    /// </summary>
    ValueTask<IReadOnlyList<SkillInfo>> ListSkillsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a skill by its identifier.
    /// </summary>
    ValueTask<SkillContent?> GetSkillAsync(
        string skillName,
        CancellationToken cancellationToken = default);
}
```

#### SkillInfo

Lightweight metadata for skill discovery:

```csharp
internal sealed class SkillInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Uri { get; init; }
    public string? Version { get; init; }
    public IReadOnlyList<SkillArgument>? Arguments { get; init; }
}

internal sealed class SkillArgument
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required bool Required { get; init; }
}
```

#### SkillContent

Full skill content for reading:

```csharp
internal sealed class SkillContent
{
    public required string Name { get; init; }
    public required string Content { get; init; }
    public string? MimeType { get; init; } = "text/markdown";
}
```

### Skill Content Sources

Skills can come from multiple sources, prioritized:

1. **Built-in skills** - Hardcoded C# implementations (current prompts as fallback)
2. **aspire.dev skills** - Fetched from llms-small.txt documentation
3. **Local skills** (future) - `~/.aspire/skills/` directory

#### AspireDevSkillsSource

Generates skills from aspire.dev documentation sections:

```csharp
internal sealed class AspireDevSkillsSource : ISkillsSource
{
    private readonly IDocsIndexService _docsIndex;

    public async ValueTask<IReadOnlyList<SkillInfo>> ListSkillsAsync(
        CancellationToken cancellationToken)
    {
        await _docsIndex.EnsureIndexedAsync(cancellationToken);
        // Map relevant documentation sections to skills
    }

    public async ValueTask<SkillContent?> GetSkillAsync(
        string skillName,
        CancellationToken cancellationToken)
    {
        // Find matching doc and build skill content
    }
}
```

### Skill Mapping Strategy

Map aspire.dev documentation to skills:

| Skill Name | Documentation Source | Description |
|------------|---------------------|-------------|
| `aspire-pair-programmer` | Built-in + tools list | Main persona with MCP tool knowledge |
| `troubleshoot-app` | troubleshooting/* docs | Systematic debugging workflow |
| `debug-resource` | Built-in workflow | Resource-specific debugging |
| `add-integration` | integrations/* docs | Integration guidance |
| `deploy-app` | deployment/* docs | Deployment workflows |
| `{integration-name}` | integration-specific doc | Per-integration skill |

### Prompt-to-Skill Delegation

Prompts delegate to skills for content, maintaining backward compatibility:

```csharp
internal sealed class TroubleshootAppPrompt : CliMcpPrompt
{
    private readonly ISkillsProvider _skills;

    public override async GetPromptResult GetPrompt(
        IReadOnlyDictionary<string, string>? arguments)
    {
        // Get skill content (may be cached)
        var skill = await _skills.GetSkillAsync("troubleshoot-app");

        // Build prompt with skill content + user arguments
        var prompt = BuildPromptFromSkill(skill, arguments);

        return new GetPromptResult { ... };
    }
}
```

## MCP Resource Handlers

### ListResourcesHandler

```csharp
handlers.ListResourcesHandler = async (request, cancellationToken) =>
{
    var skills = await _skillsProvider.ListSkillsAsync(cancellationToken);

    return new ListResourcesResult
    {
        Resources = skills.Select(s => new Resource
        {
            Uri = s.Uri,
            Name = s.Name,
            Description = s.Description,
            MimeType = "text/markdown"
        }).ToList()
    };
};
```

### ReadResourceHandler

```csharp
handlers.ReadResourceHandler = async (request, cancellationToken) =>
{
    var uri = request.Params?.Uri;
    if (uri is null || !uri.StartsWith("skill://"))
    {
        throw new McpProtocolException("Invalid skill URI", McpErrorCode.InvalidParams);
    }

    var skillName = uri.Substring("skill://".Length);
    var content = await _skillsProvider.GetSkillAsync(skillName, cancellationToken);

    if (content is null)
    {
        throw new McpProtocolException($"Skill not found: {skillName}", McpErrorCode.ResourceNotFound);
    }

    return new ReadResourceResult
    {
        Contents = [new TextResourceContents
        {
            Uri = uri,
            Text = content.Content,
            MimeType = content.MimeType
        }]
    };
};
```

## Skill Content Format

Skills use markdown format with optional YAML frontmatter:

```markdown
---
name: add-integration
description: Step-by-step guidance for adding Aspire integrations
version: 1.0.0
arguments:
  - name: integrationType
    description: Type of integration (redis, postgresql, etc.)
    required: true
  - name: resourceName
    description: Name for the resource
    required: false
---

# Add Integration Skill

You are helping the user add a new Aspire integration to their application.

## Available Tools

Use these MCP tools to assist:
- `search_docs` - Find integration documentation
- `list_integrations` - List available integrations
- `get_doc` - Get detailed integration docs

## Workflow

1. **Identify integration type** from user request
2. **Search documentation** using `search_docs`
3. **Provide NuGet packages** required for both hosting and client
4. **Show AppHost configuration** code
5. **Show client configuration** code

...
```

## Built-in Skills

Some skills require MCP-specific knowledge (available tools, server capabilities) that isn't in documentation. These are built-in:

### aspire-pair-programmer

Main persona skill with:
- Aspire architecture knowledge
- Available MCP tools and when to use them
- CLI vs dotnet CLI guidance
- Integration recommendations

### debug-resource

Resource debugging workflow with:
- Step-by-step debugging process
- Tool usage patterns for logs/traces
- Common error patterns

## Caching Strategy

Skills are cached at multiple levels:

1. **Documentation cache** - DocsIndexService caches parsed llms.txt
2. **Skill content cache** - SkillsProvider caches generated skill content
3. **ETag validation** - Documentation freshness checked via HTTP ETag

Cache invalidation triggers:
- Documentation ETag change (new aspire.dev content)
- Explicit cache clear (restart, manual)

## Implementation Plan

### Phase 1: Infrastructure

1. Add `ISkillsProvider` interface and implementation
2. Add `ListResourcesHandler` and `ReadResourceHandler` to MCP server
3. Expose built-in skills as resources

### Phase 2: Dynamic Skills

1. Create `AspireDevSkillsSource` to generate skills from documentation
2. Map documentation sections to skill URIs
3. Build skill content with appropriate context

### Phase 3: Prompt Migration

1. Migrate prompts to use `ISkillsProvider` for content
2. Add skill argument interpolation
3. Maintain backward compatibility

### Phase 4: Extensibility (Future)

1. Add local skills source (`~/.aspire/skills/`)
2. Support skill versioning
3. Add skill subscriptions for change notifications

## File Locations

```directory
└───📂 Mcp
     ├───📂 Skills (NEW)
     │    ├─── AspireDevSkillsSource.cs
     │    ├─── BuiltInSkillsSource.cs
     │    ├─── ISkillsProvider.cs
     │    ├─── ISkillsSource.cs
     │    ├─── SkillContent.cs
     │    ├─── SkillInfo.cs
     │    └─── SkillsProvider.cs
     ├───📂 Docs
     │    ├─── DocsCache.cs
     │    ├─── DocsFetcher.cs
     │    ├─── DocsIndexService.cs
     │    └─── LlmsTxtParser.cs
     └───📂 Prompts
          └─── (existing, updated to use ISkillsProvider)
```

## Testing Strategy

1. **Unit tests** for skill parsing and content generation
2. **Unit tests** for resource URI handling
3. **Integration tests** for skill discovery flow
4. **End-to-end tests** for prompt-to-skill delegation

## Security Considerations

- **Read-only** - Skills are read-only, no modification via MCP
- **URI validation** - Strict validation of skill URIs
- **Content sanitization** - Skill content sanitized before serving
- **No arbitrary URLs** - Skills only from trusted sources (aspire.dev, built-in)

## Future Considerations

### Local Skills Support

```
~/.aspire/skills/
├── my-custom-workflow/
│   └── SKILL.md
└── team-debugging/
    └── SKILL.md
```

### Skill Versioning

Track skill versions for debugging and compatibility:

```csharp
public sealed class SkillInfo
{
    public string? Version { get; init; }
    public DateTimeOffset? LastUpdated { get; init; }
    public string? SourceUri { get; init; }  // e.g., aspire.dev URL
}
```

### Skill Subscriptions

Allow clients to subscribe to skill updates:

```csharp
handlers.SubscribeToResourcesHandler = async (request, cancellationToken) =>
{
    // Track client interest in skill://* URIs
    // Send notifications when skills update
};
```

## References

- [FastMCP SkillsProvider](https://github.com/jlowin/fastmcp/tree/main/src/fastmcp/server/providers/skills)
- [MCP Resources Specification](https://modelcontextprotocol.io/docs/concepts/resources)
- [Aspire MCP Documentation Search](./mcp-docs-search.md)
