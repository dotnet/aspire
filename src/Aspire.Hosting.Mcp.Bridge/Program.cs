// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Mcp.Bridge;

var builder = WebApplication.CreateBuilder(args);

// Get logger for startup logging
var loggerFactory = LoggerFactory.Create(logging => logging.AddConsole());
var startupLogger = loggerFactory.CreateLogger("MCP.Bridge.Startup");

startupLogger.LogInformation("MCP Bridge starting...");

// Log configuration
var mcpCommand = builder.Configuration["MCP_SERVER_COMMAND"];
var mcpArgs = builder.Configuration["MCP_SERVER_ARGS"];
var mcpWorkingDir = builder.Configuration["MCP_SERVER_WORKING_DIRECTORY"];

startupLogger.LogInformation("MCP Server Command: {Command}", mcpCommand ?? "(not set)");
startupLogger.LogInformation("MCP Server Args: {Args}", mcpArgs ?? "(not set)");
if (!string.IsNullOrEmpty(mcpWorkingDir))
{
    startupLogger.LogInformation("MCP Server Working Directory: {WorkingDir}", mcpWorkingDir);
}

// Configure port from PORT environment variable if available
var port = builder.Configuration["PORT"];
if (!string.IsNullOrEmpty(port))
{
    startupLogger.LogInformation("Configuring Kestrel to listen on port {Port}", port);
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(int.Parse(port, System.Globalization.CultureInfo.InvariantCulture));
    });
}
else
{
    startupLogger.LogWarning("PORT environment variable not set, using default Kestrel configuration");
}

// Add services
builder.Services.AddSingleton<StdioMcpProxyService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<StdioMcpProxyService>());

var app = builder.Build();

// MCP endpoint
app.MapPost("/mcp", async (HttpContext context, StdioMcpProxyService proxyService, CancellationToken cancellationToken) =>
{
    try
    {
        await proxyService.HandleRequestAsync(context, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        context.RequestServices.GetRequiredService<ILogger<Program>>().LogError(ex, "Error handling MCP request");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new JsonRpcErrorResponse
        {
            Error = new JsonRpcError
            {
                Code = -32603,
                Message = "Internal server error"
            },
            Id = null
        }, McpBridgeJsonContext.Default.JsonRpcErrorResponse, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
});

// Health check endpoint
app.MapGet("/health", (StdioMcpProxyService proxyService) => 
    proxyService.IsStarted 
        ? Results.Json(new HealthResponse(), McpBridgeJsonContext.Default.HealthResponse)
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

startupLogger.LogInformation("MCP Bridge endpoints configured, starting server...");

await app.RunAsync().ConfigureAwait(false);
