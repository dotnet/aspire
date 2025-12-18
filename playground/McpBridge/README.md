# MCP Bridge Playground

This playground demonstrates how to use the Aspire MCP Bridge to integrate stdio-based MCP servers with the Aspire Dashboard.

## What is MCP Bridge?

The MCP Bridge allows you to use stdio-based Model Context Protocol (MCP) servers with the Aspire Dashboard, which only supports HTTP-based MCP connections. The bridge spawns a stdio MCP server process and exposes it via an HTTP endpoint that the Dashboard can discover and connect to.

## Running the Sample

1. Ensure you have Node.js installed (required for the memory MCP server example)
2. Run the AppHost project:
   ```bash
   dotnet run --project McpBridge.AppHost
   ```
3. Open the Aspire Dashboard
4. The memory MCP tools should appear in the MCP tools list

## How It Works

The `AddNpxMcpBridge` extension method:
1. Creates an McpBridgeResource that runs the Aspire.Hosting.Mcp.Bridge application
2. The bridge application spawns the npx MCP server (`@modelcontextprotocol/server-memory`)
3. The bridge exposes an HTTP endpoint that proxies requests to the stdio MCP server
4. The Dashboard discovers the HTTP endpoint and can call the MCP tools

## Examples

### NPX-based MCP Server
```csharp
var memoryMcp = builder.AddNpxMcpBridge(
    "memory-mcp",
    "@modelcontextprotocol/server-memory")
    .WithMcpNamespace("memory");
```

### Custom Node.js MCP Server
```csharp
var customMcp = builder.AddMcpBridge(
    "custom-mcp",
    "node",
    ["path/to/your/mcp-server.js"])
    .WithWorkingDirectory("path/to/working/dir")
    .WithMcpNamespace("custom");
```

### Python MCP Server
```csharp
var pythonMcp = builder.AddPythonMcpBridge(
    "python-mcp",
    "my_mcp_module")
    .WithMcpNamespace("python");
```

### With API Key Authentication
```csharp
var secureMcp = builder.AddNpxMcpBridge(
    "secure-mcp",
    "@modelcontextprotocol/server-memory")
    .WithApiKey("my-secret-key")
    .WithMcpNamespace("memory");
```

## Available MCP Tools

The memory MCP server provides tools for:
- Creating entities and relations in a knowledge graph
- Reading the graph
- Deleting from the graph

Try using these tools from the Aspire Dashboard's MCP interface!
