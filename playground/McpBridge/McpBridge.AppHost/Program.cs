// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

// Add an npx-based memory MCP server via the bridge
builder.AddNpxMcpBridge(
    "memory-mcp",
    "@modelcontextprotocol/server-memory")
    .WithMcpNamespace("memory");

// You can also add custom MCP servers
// builder.AddMcpBridge(
//     "custom-mcp",
//     "node",
//     ["path/to/your/mcp-server.js"])
//     .WithWorkingDirectory("path/to/working/dir")
//     .WithMcpNamespace("custom");

// Or Python-based MCP servers
// builder.AddPythonMcpBridge(
//     "python-mcp",
//     "my_mcp_module")
//     .WithMcpNamespace("python");

builder.Build().Run();
