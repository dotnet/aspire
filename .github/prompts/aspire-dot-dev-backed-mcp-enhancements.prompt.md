# Plan: Aspire MCP Server Documentation & Search Enhancement

Enhance the Aspire MCP server with dynamic `aspire.dev` documentation fetching, in-memory vector search, prompts, and an Aspire pair programmer persona—all delegating to the Aspire CLI where possible. Adding caching, embedding, and search tools to improve developer experience. Employ existing patterns from Aspire CLI and MCP server for consistency. Consider implementing a jump-priority fetch for full docs based on user queries. Crawl, embed, and cache docs in the background for responsive fetch/search.

## Specification

Formal specification: [docs/specs/mcp-docs-search.md](docs/specs/mcp-docs-search.md)

## Completed Steps

1. ✅ **Add `IMemoryCache` caching layer for doc content** — Created `IDocsCache` and `DocsCache` in [src/Aspire.Cli/Mcp/Docs/](src/Aspire.Cli/Mcp/Docs/) using `IMemoryCache` with configurable TTL (default: 1 hour for content, 4 hours for chunks).

2. ✅ **Create `FetchAspireDocsTool` and `SearchAspireDocsTool` MCP tools** — Added tools extending `CliMcpTool` that fetch from `https://aspire.dev/llms.txt`, download `llms-small.txt` or `llms-full.txt`, and cache via `IDocsCache`.

3. ✅ **Implement in-memory vector embedding for semantic search** — Added `DocsEmbeddingService` with:
   - Document chunking (1000 chars, 200 overlap, section-aware)
   - `IEmbeddingGenerator<string, Embedding<float>>` from Microsoft.Extensions.AI
   - `TensorPrimitives.CosineSimilarity` for hardware-accelerated similarity
   - Keyword fallback when no embedding provider configured

4. ✅ **Expose MCP prompts for Aspire pair programmer persona** — Added to [McpStartCommand](src/Aspire.Cli/Commands/McpStartCommand.cs):
   - `ListPromptsHandler` and `GetPromptHandler` registration
   - Prompts: `aspire_pair_programmer`, `debug_resource`, `add_integration`, `deploy_app`, `troubleshoot_app`
   - `CliMcpPrompt` base class in [src/Aspire.Cli/Mcp/Prompts/](src/Aspire.Cli/Mcp/Prompts/)

5. ✅ **Wire tools to delegate CLI operations** — Tools use Aspire CLI patterns from [KnownMcpTools](src/Aspire.Cli/Mcp/KnownMcpTools.cs). No dotnet CLI usage.

6. ✅ **Add tool registration and update known tools** — Registered in `_knownTools`, added to `KnownMcpTools.cs`, classified as local tools.

## Pending Steps

7. 🔲 **Enhance `get_integration_docs` to use doc search services** — Update [GetIntegrationDocsTool](src/Aspire.Cli/Mcp/GetIntegrationDocsTool.cs):
   - Inject `IDocsFetcher` and `IDocsEmbeddingService` dependencies
   - Search aspire.dev docs for integration-specific content (e.g., "Aspire.Hosting.Redis usage examples")
   - Return documentation snippets alongside NuGet URL
   - Include code examples and configuration guidance from docs
   - Add optional `includeDocSnippets` parameter (default: true)

8. 🔲 **Add background indexing with priority queue**:
   - Start indexing small docs on first tool use
   - Queue full docs for background processing via `IHostedService`
   - Support "jump priority" for specific topics

9. 🔲 **Add embedding provider configuration** via environment:
   - `ASPIRE_EMBEDDING_PROVIDER=ollama|azure|openai`
   - `ASPIRE_EMBEDDING_MODEL=all-minilm|text-embedding-ada-002`
   - `ASPIRE_EMBEDDING_ENDPOINT=http://localhost:11434`

10. 🔲 **Optional disk persistence for embeddings** using `IDiskCache`:
    - Serialize embeddings to avoid regeneration across sessions
    - Cache invalidation based on doc version/timestamp

## Further Considerations

1. **Embedding provider configuration?** Environment variables vs explicit DI registration vs CLI options?

2. **Cache persistence?** In-memory only vs `IDiskCache`? TTL and invalidation strategies?

3. **Full vs small docs strategy?** Small-first with background full-indexing? Jump-priority for specific topics?
