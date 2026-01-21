# Aspire MCP Documentation Search and Embedding Services

## Overview

This specification describes the design and implementation of documentation fetching, caching, embedding, and semantic search capabilities for the Aspire MCP server. These features enable AI agents and developers to query aspire.dev documentation using natural language, powered by vector embeddings and cosine similarity search.

## Background

### Motivation

The Aspire MCP server provides tools for interacting with Aspire applications, but lacks direct access to Aspire documentation. Developers and AI agents often need to:

1. **Find documentation** - Locate relevant Aspire documentation for specific features, APIs, or concepts
2. **Get context-aware answers** - Retrieve documentation snippets that are semantically relevant to their questions
3. **Stay current** - Access up-to-date documentation from aspire.dev without manual lookups

### Prior Art

- **aspire.dev/llms-small.txt** - The Aspire documentation site exposes an LLM-friendly documentation file at `https://aspire.dev/llms-small.txt` containing abridged documentation suitable for AI agent consumption
- **Existing integration docs tool** - The `get_integration_docs` tool exists but only returns NuGet package README.md, not actual documentation content

## Design Goals

### Primary Objectives

1. **Dynamic documentation fetching** - Fetch aspire.dev documentation on-demand with appropriate caching
2. **Semantic search capability** - Enable natural language queries against documentation using vector embeddings
3. **Graceful degradation** - Work without an embedding provider (falls back to keyword search)
4. **Aspire pair programmer persona** - Provide prompts that guide AI agents to use documentation effectively
5. **CLI delegation** - Prefer Aspire CLI commands over reimplementing functionality
6. **Avoid ambiguity with .NET CLI** - Ensure all operations are clearly tied to Aspire CLI

### Non-Goals

- **Offline-first** - Not designed for fully offline scenarios (requires network for initial fetch)
- **Full-text indexing** - Not a replacement for a full search engine
- **Persistent vector store** - Embeddings are cached in-memory, not persisted to disk

## Architecture

### Component Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         MCP Server                                  │
├─────────────────────────────────────────────────────────────────────┤
│  Tools                             │  Prompts                       │
│  ├─ fetch_aspire_docs              │  ├─ aspire_pair_programmer     │
│  ├─ search_aspire_docs             │  ├─ debug_resource             │
│  └─ get_integration_docs (updated) │  ├─ add_integration            │
│                                    │  ├─ deploy_app                 │
│                                    │  └─ troubleshoot_app           │
├─────────────────────────────────────────────────────────────────────┤
│  Services                                                           │
│  ├─ IDocsFetcher      - HTTP client for aspire.dev docs             │
│  ├─ IDocsCache        - IMemoryCache wrapper for docs + chunks      │
│  └─ IDocsEmbeddingService - Chunking, embedding, similarity search  │
└─────────────────────────────────────────────────────────────────────┘
```

### Service Interfaces

#### IDocsFetcher

Fetches documentation content from aspire.dev:

```csharp
internal interface IDocsFetcher
{
    Task<string?> FetchSmallDocsAsync(CancellationToken cancellationToken = default);
}
```

**Implementation details:**
- Base URL: `https://aspire.dev/`
- Endpoint: `llms-small.txt`
- Uses `HttpClient` with appropriate timeout and error handling
- Caches results via `IDocsCache` with configurable TTL (default: 4 hours)

#### IDocsCache

Provides caching for documentation content and embedding chunks:

```csharp
internal interface IDocsCache
{
    Task<string?> GetContentAsync(string key, CancellationToken cancellationToken = default);
    Task SetContentAsync(string key, string content, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocChunk>?> GetChunksAsync(string key, CancellationToken cancellationToken = default);
    Task SetChunksAsync(string key, IReadOnlyList<DocChunk> chunks, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
}
```

**Implementation details:**
- Wraps `IMemoryCache` from `Microsoft.Extensions.Caching.Memory`
- Default TTL: 1 hour for content, 4 hours for embedded chunks
- Keys are prefixed to avoid collisions (e.g., `aspire_docs_content_`, `aspire_docs_chunks_`)

#### IDocsEmbeddingService

Provides document chunking, embedding generation, and similarity search:

```csharp
internal interface IDocsEmbeddingService
{
    bool IsConfigured { get; }
    Task<int> IndexDocumentAsync(string content, string sourceKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
}
```

**Implementation details:**
- Uses `IEmbeddingGenerator<string, Embedding<float>>` from `Microsoft.Extensions.AI`
- Chunking strategy: 1000 characters with 200 character overlap
- Preserves markdown section headers for context
- Cosine similarity computed via `System.Numerics.Tensors.TensorPrimitives.CosineSimilarity` (hardware-accelerated)
- Falls back gracefully when no embedding generator is configured

### Document Chunking Strategy

Documents are chunked to balance context preservation with embedding quality:

1. **Section-aware splitting** - Split on markdown headers (`#`, `##`, `###`) to preserve topic boundaries
2. **Overlap** - 200-character overlap between chunks to maintain context across boundaries
3. **Chunk size** - Target 1000 characters per chunk (configurable)
4. **Metadata preservation** - Each chunk stores source file and section header context

```csharp
internal sealed class DocChunk
{
    public required string Content { get; init; }
    public required string Source { get; init; }
    public string? Section { get; init; }
    public float[]? Embedding { get; set; }
}
```

### Similarity Search

Search uses cosine similarity between query embedding and document chunk embeddings:

1. Generate embedding for query using `IEmbeddingGenerator`
2. Compute cosine similarity: `TensorPrimitives.CosineSimilarity(queryVector, chunkVector)`
3. Rank by similarity score (higher = more relevant)
4. Return top-K results with content, source, section, and score

**Fallback behavior:**
When no embedding generator is configured, falls back to keyword search:
- Tokenize query into terms
- Score paragraphs by term frequency
- Return top-K matches

## MCP Tools

### fetch_aspire_docs

Fetches documentation content from aspire.dev.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "query": {
      "type": "string",
      "description": "A brief description of what you're looking for in the documentation."
    }
  },
  "required": ["query"],
  "additionalProperties": false,
  "description": "Fetches aspire.dev documentation content based on the provided query context."
}
```

**Behavior:**
- Accepts a query parameter describing what the user is looking for
- Returns the abridged documentation from `llms-small.txt`
- Content is cached for subsequent requests
- Suitable for quick lookups and general questions

### search_aspire_docs

Performs semantic search across indexed documentation.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "query": {
      "type": "string",
      "description": "The natural language search query."
    },
    "topK": {
      "type": "integer",
      "description": "Number of results to return (default: 5, max: 20)."
    }
  },
  "required": ["query"]
}
```

**Behavior:**
1. Auto-indexes small docs if not already indexed
2. Generates query embedding
3. Returns ranked results with content snippets and similarity scores
4. Falls back to keyword search if embedding generator not configured

### get_integration_docs (Enhanced)

The existing `get_integration_docs` tool is enhanced to provide richer documentation:

**Current behavior:**
- Returns only NuGet package README.md content

**Enhanced behavior:**
1. Search aspire.dev docs for integration-specific content using `search_aspire_docs`
2. Return relevant documentation snippets
3. Include code examples and configuration guidance when available

## MCP Prompts

### aspire_pair_programmer

Main persona prompt that activates an Aspire expert assistant.

**Arguments:**
- `context` (optional) - What the user is working on

**Behavior:**
- Provides system context about Aspire architecture, integrations, and best practices
- Lists available MCP tools and when to use them
- Emphasizes using Aspire CLI (not dotnet CLI) for operations
- Guides toward documentation-backed answers

### debug_resource

Workflow prompt for debugging resource issues.

**Arguments:**
- `resourceName` (required) - Resource to debug
- `issue` (optional) - Description of the problem

**Behavior:**
- Guides through systematic debugging: status → logs → traces → recommendations

### add_integration

Workflow prompt for adding new integrations.

**Arguments:**
- `integrationType` (required) - Type of integration (redis, postgresql, etc.)
- `resourceName` (optional) - Name for the resource

**Behavior:**
- Searches for integration packages
- Fetches documentation
- Provides AppHost and client configuration guidance

### deploy_app

Workflow prompt for deployment guidance.

**Arguments:**
- `target` (required) - Deployment target (azure, kubernetes, docker-compose)
- `environment` (optional) - Target environment name

**Behavior:**
- Runs environment checks via `doctor`
- Fetches deployment documentation
- Guides through `aspire publish` and `aspire deploy` workflows

### troubleshoot_app

Comprehensive troubleshooting prompt.

**Arguments:**
- `symptom` (required) - Description of the issue

**Behavior:**
- Systematic analysis: environment → resources → logs → traces → docs → recommendations

## Configuration

### Embedding Provider

The embedding service uses optional dependency injection for the embedding generator:

```csharp
// In DI registration (Program.cs or startup)
services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
    new OllamaEmbeddingGenerator(new Uri("http://localhost:11434"), "all-minilm"));

// Or for Azure OpenAI
services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
    new AzureOpenAIClient(endpoint, credential)
        .AsEmbeddingGenerator("text-embedding-ada-002"));
```

**Without configuration:**
- `IDocsEmbeddingService.IsConfigured` returns `false`
- `search_aspire_docs` falls back to keyword search
- No indexing occurs

### Cache TTL Configuration

Default TTL values:
- Documentation content: 1 hour
- Embedded chunks: 4 hours

Can be customized via service registration or future configuration options.

## Future Considerations

### Background Indexing

Consider implementing background indexing with priority queuing:
- Start indexing small docs immediately on first tool use
- Support progressive loading for larger documentation sources if needed in the future

### Disk Persistence

Consider adding optional disk persistence for embeddings:
- Use existing `IDiskCache` pattern
- Serialize embeddings to avoid regeneration across sessions
- Implement cache invalidation based on documentation version/timestamp

### Embedding Provider Configuration

Consider adding CLI/environment configuration for embedding providers:
- `ASPIRE_EMBEDDING_PROVIDER=ollama|azure|openai`
- `ASPIRE_EMBEDDING_MODEL=all-minilm|text-embedding-ada-002`
- `ASPIRE_EMBEDDING_ENDPOINT=http://localhost:11434`

### Integration with get_integration_docs

The `get_integration_docs` tool should be enhanced to:
1. Accept optional `query` parameter for semantic search within integration docs
2. Use `IDocsEmbeddingService` to find relevant documentation snippets
3. Return structured response with NuGet info + documentation excerpts

## Implementation Notes

### File Locations

```
src/Aspire.Cli/
├── Mcp/
│   ├── Docs/
│   │   ├── IDocsCache.cs
│   │   ├── DocsCache.cs
│   │   ├── DocsFetcher.cs
│   │   └── DocsEmbeddingService.cs
│   ├── Prompts/
│   │   ├── KnownMcpPrompts.cs
│   │   ├── CliMcpPrompt.cs
│   │   ├── AspirePairProgrammerPrompt.cs
│   │   ├── DebugResourcePrompt.cs
│   │   ├── AddIntegrationPrompt.cs
│   │   ├── DeployAppPrompt.cs
│   │   └── TroubleshootAppPrompt.cs
│   ├── FetchAspireDocsTool.cs
│   ├── SearchAspireDocsTool.cs
│   └── KnownMcpTools.cs (updated)
└── Commands/
    └── McpStartCommand.cs (updated)
```

### Dependencies

- `Microsoft.Extensions.Caching.Memory` - Already in project
- `Microsoft.Extensions.AI` - For `IEmbeddingGenerator` abstraction
- `System.Numerics.Tensors` - For `TensorPrimitives.CosineSimilarity`

### Testing Strategy

1. **Unit tests** for chunking logic and keyword fallback search
2. **Integration tests** with mock `IEmbeddingGenerator` for search behavior
3. **Manual testing** with real embedding providers (Ollama, Azure OpenAI)

## References

- [aspire.dev/llms-small.txt](https://aspire.dev/llms-small.txt) - Abridged documentation for LLMs
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/) - AI abstractions for .NET
- [MCP Specification](https://modelcontextprotocol.io/) - Model Context Protocol
- [TensorPrimitives.CosineSimilarity](https://learn.microsoft.com/dotnet/api/system.numerics.tensors.tensorprimitives.cosinesimilarity) - Hardware-accelerated similarity computation
