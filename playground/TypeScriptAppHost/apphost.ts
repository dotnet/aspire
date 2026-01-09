// Aspire TypeScript AppHost - Capability-based API Demo
// This demonstrates the new ATS capability-based API with fluent builder pattern.
// Run with: aspire run

import { createBuilder, refExpr, EnvironmentCallbackContext } from './.modules/aspire.js';

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

// Add container with environment callback to demonstrate the new callback API
// Note: The callback receives a handle. Future improvement: auto-wrap into context class.
const api = await builder
    .addContainer("api", "mcr.microsoft.com/dotnet/samples:aspnetapp")
    .withEnvironmentCallback(async (ctx: EnvironmentCallbackContext) => {
        console.log(`  📋 Environment callback invoked for API container`);

        // TODO: Once the code generator wraps handles into context classes:
        // const execContext = await ctx.executionContext();
        // const vars = await ctx.environmentVariables();
        // await vars.set("MY_CUSTOM_VAR", "Hello from TypeScript!");
    });
console.log("✅ Added API container with environment callback");

// Build and run - fully fluent!
console.log("\n🚀 Building and running...\n");
await builder.build().run();
