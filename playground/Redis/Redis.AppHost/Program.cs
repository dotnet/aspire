var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");
redis.WithDataVolume()
    .WithRedisCommander(c => c.WithHostPort(33803).WithParentRelationship(redis))
    .WithRedisInsight(c => c.WithHostPort(41567).WithParentRelationship(redis));

// Add stdio-based Redis MCP server using the MCP bridge
// The connection URL will be injected via command-line argument
// Use factory overload so UriExpression is evaluated after BeforeStartEvent (when TlsEnabled is set)
var redisMcp = builder.AddMcpBridge(
    "redis-mcp",
    "uvx",
    ["--from", "redis-mcp-server@latest", "redis-mcp-server"])
    .WithMcpNamespace("redis")
    .WithServerArgument("--url", () => redis.Resource.UriExpression)
    .WaitFor(redis);

var garnet = builder.AddGarnet("garnet")
    .WithDataVolume();

var valkey = builder.AddValkey("valkey")
    .WithDataVolume("valkey-data");

builder.AddProject<Projects.Redis_ApiService>("apiservice")
    .WithExternalHttpEndpoints()
    .WithReference(redis).WaitFor(redis)
    .WithReference(garnet).WaitFor(garnet)
    .WithReference(valkey).WaitFor(valkey);

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
