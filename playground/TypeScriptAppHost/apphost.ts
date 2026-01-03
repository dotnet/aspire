// Aspire TypeScript AppHost - Capability-based API Demo
// This demonstrates the new ATS capability-based API.
// Run with: aspire run (after renaming to apphost.ts)

import { connect, AspireClient, BuilderHandle, ContainerBuilderHandle } from './.modules/aspire.js';

console.log("Aspire TypeScript AppHost (Capability API) starting...");

// Connect to the Aspire host
const aspire = await connect();

// List available capabilities
const capabilities = await aspire.getCapabilities();
console.log(`✅ Connected! ${capabilities.length} capabilities available`);
console.log(`   Sample capabilities: ${capabilities.slice(0, 5).join(', ')}...`);

// ============================================================================
// Create the distributed application builder
// ============================================================================
const builder = await aspire.createBuilder();
console.log(`✅ Created builder: ${builder.$handle}`);

// ============================================================================
// Check execution context
// ============================================================================
const context = await aspire.getExecutionContext(builder);
const isRunMode = await aspire.isRunMode(context);
const isPublishMode = await aspire.isPublishMode(context);
console.log(`✅ Execution context: isRunMode=${isRunMode}, isPublishMode=${isPublishMode}`);

// ============================================================================
// Add a Redis container
// ============================================================================
let redis = await aspire.addContainer(builder, "cache", "redis:latest");
console.log(`✅ Added Redis container: ${redis.$handle}`);

// Configure with environment variables (chaining style)
redis = await aspire.withEnvironment(redis, "REDIS_VERSION", "latest");
redis = await aspire.withEnvironment(redis, "REDIS_MODE", "standalone");
console.log(`✅ Configured Redis with environment variables`);

// Add TCP endpoint (Redis uses port 6379)
redis = await aspire.withHttpEndpoint(redis, { targetPort: 6379, name: "tcp" });
console.log(`✅ Added TCP endpoint to Redis`);

// ============================================================================
// Add another container (API)
// ============================================================================
let api = await aspire.addContainer(builder, "api", "mcr.microsoft.com/dotnet/samples:aspnetapp");
console.log(`✅ Added API container: ${api.$handle}`);

// Configure API with environment variables
api = await aspire.withEnvironment(api, "ASPNETCORE_URLS", "http://+:8080");
api = await aspire.withHttpEndpoint(api, { targetPort: 8080, name: "http" });
console.log(`✅ Configured API container`);

// Note: withReference and waitFor require specific resource types
// (e.g., Redis resource, not generic containers) to work properly
// Skip for this demo with generic containers

// ============================================================================
// Get resource info
// ============================================================================
const redisName = await aspire.getResourceName(redis);
const apiName = await aspire.getResourceName(api);
console.log(`✅ Resource names: ${redisName}, ${apiName}`);

// ============================================================================
// Build and run the application
// ============================================================================
console.log("\n🚀 Building and running the application...\n");

const app = await aspire.build(builder);
console.log(`✅ Application built: ${app.$handle}`);

await aspire.run(app);
