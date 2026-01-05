// Aspire TypeScript AppHost - Capability-based API Demo
// This demonstrates the new ATS capability-based API with fluent builder pattern.
// Run with: aspire run

import { createBuilder, refExpr } from './.modules/aspire.js';

console.log("Aspire TypeScript AppHost starting...\n");

// Create the distributed application builder
const builder = await createBuilder();
console.log("✅ Created builder");

// Add resources using fluent chaining
const cache = await builder
    .addRedis("cache")
    .withRedisCommander();
console.log("✅ Added Redis with Commander");

// Demonstrate reference expression creation using tagged template literal
// This creates a dynamic connection string that references the endpoint at runtime
const port = 6379;
const redisUrl = refExpr`redis://localhost:${port}`;
console.log(`✅ Created reference expression: ${redisUrl}`);

const api = await builder
    .addContainer("api", "mcr.microsoft.com/dotnet/samples:aspnetapp");
console.log("✅ Added API container");

// Build and run - fully fluent!
console.log("\n🚀 Building and running...\n");
await builder.build().run();
