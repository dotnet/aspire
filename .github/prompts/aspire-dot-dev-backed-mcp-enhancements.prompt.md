# Plan: Aspire MCP Server Documentation & Search Enhancement

Enhance the Aspire MCP server with dynamic `aspire.dev` documentation fetching, lexical search, prompts, and an Aspire pair programmer persona—all delegating to the Aspire CLI where possible. Adding caching and search tools to improve developer experience. Employ existing patterns from Aspire CLI and MCP server for consistency.

## Specification

Formal specification: [docs/specs/mcp-docs-search.md](docs/specs/mcp-docs-search.md)

## Completed Steps

1. ✅ **Add caching layer for doc content with ETag validation** — Created `IDocsCache` and `DocsCache` in [src/Aspire.Cli/Mcp/Docs/](src/Aspire.Cli/Mcp/Docs/) using `IMemoryCache` with ETag-based cache invalidation (conditional HTTP requests with `If-None-Match` header).

2. ✅ **Create documentation MCP tools** — Added three tools extending `CliMcpTool`:
   - `list_docs` - Lists all available documents from aspire.dev
   - `search_docs` - Performs weighted lexical search across documentation
   - `get_doc` - Retrieves a specific document by slug

3. ✅ **Implement optimized lexical search** — Added `DocsIndexService` with:
   - Async parallel document parsing via `LlmsTxtParser.ParseAsync`
   - Pre-indexing with `IndexedDocument` and `IndexedSection` classes for pre-computed lowercase text
   - Weighted field scoring (title: 10x, summary: 8x, heading: 6x, code: 5x, body: 1x)
   - Span-based string operations for zero-allocation searching
   - Code identifier extraction for bonus scoring
   - Static lambdas to avoid closure allocations

4. ✅ **Implement LlmsTxtParser with async parallel processing** — Added [LlmsTxtParser](src/Aspire.Cli/Mcp/Docs/LlmsTxtParser.cs):
   - `ParseAsync` method with `Task.WhenAll` for parallel document parsing
   - Span-based operations throughout for minimal allocations
   - `ArrayPool<char>` for slug generation
   - Section-aware parsing (H1 = document boundary, H2+ = sections)

5. ✅ **Expose MCP prompts for Aspire pair programmer persona** — Added to [McpStartCommand](src/Aspire.Cli/Commands/McpStartCommand.cs):
   - `ListPromptsHandler` and `GetPromptHandler` registration
   - Prompts: `aspire_pair_programmer`, `debug_resource`, `add_integration`, `deploy_app`, `troubleshoot_app`
   - `CliMcpPrompt` base class in [src/Aspire.Cli/Mcp/Prompts/](src/Aspire.Cli/Mcp/Prompts/)

6. ✅ **Wire tools to delegate CLI operations** — Tools use Aspire CLI patterns from [KnownMcpTools](src/Aspire.Cli/Mcp/KnownMcpTools.cs). No dotnet CLI usage.

7. ✅ **Add tool registration and update known tools** — Registered in `_knownTools`, added to `KnownMcpTools.cs`, classified as local tools.

8. ✅ **Eager documentation indexing on MCP server start** — Indexing begins immediately when MCP server starts via fire-and-forget `Task.Run`, ensuring docs are ready by first query.

## Architecture Decisions

### Lexical Search over Embeddings

Chose weighted lexical search instead of vector embeddings because:
- **Zero external dependencies** - No embedding provider required (Ollama, Azure OpenAI, etc.)
- **Simpler deployment** - Works out of the box without configuration
- **Sufficient for documentation** - Documentation has well-structured headings and terminology
- **Lower latency** - No embedding generation overhead

### ETag-based Cache Invalidation

Uses HTTP conditional requests instead of TTL-based expiration:
- Sends `If-None-Match` header with cached ETag
- Returns cached content on `304 Not Modified` response
- Only re-fetches when documentation actually changes
- Falls back to cached content on network errors

## Future Considerations

1. **Full docs support** - Currently uses `llms-small.txt`; could add `llms-full.txt` for more comprehensive content

2. **Disk persistence** - Cache could persist to disk to survive process restarts

3. **Search result ranking** - Could add TF-IDF or BM25 for more sophisticated ranking
