// Aspire TypeScript AppHost - Capability-based API Demo
// This demonstrates the new ATS capability-based API with fluent builder pattern.
// Run with: aspire run

import { createBuilder, refExpr, EnvironmentCallbackContext } from './.modules/aspire.js';

console.log("Aspire TypeScript AppHost starting...\n");

// Create the distributed application builder
const builder = await createBuilder();
console.log("Created builder");

// Add resources using fluent chaining
// Note: .withEnvironment() on Redis demonstrates 2-pass scanning fix
// (withEnvironment is defined in Aspire.Hosting, RedisResource in Aspire.Hosting.Redis)
const cache = await builder
    .addRedis("cache")
    .withRedisCommander();

var ep = await cache.getEndpoint("tcp");
console.log("Added Redis with Commander and environment variable");

// Demonstrate reference expression creation using tagged template literal
// This creates a dynamic connection string that references the endpoint at runtime
const redisUrl = refExpr`redis://${ep}`;
console.log(`Created reference expression: ${redisUrl}`);

// Add container with environment callback to demonstrate the new property-like object API
// Note: .waitFor(cache) and .withReference(cache) demonstrate the union type fix
const api = await builder
    .addContainer("api", "mcr.microsoft.com/dotnet/samples:aspnetapp")
    .withEnvironmentCallback(async (ctx: EnvironmentCallbackContext) => {
        console.log(`  Environment callback invoked for API container`);

        // Use property-like object pattern to get execution context
        const execContext = await ctx.executionContext.get();
        const isRunMode = await execContext.isRunMode.get();
        console.log(`    Running in ${isRunMode ? 'run' : 'publish'} mode`);

        // Set environment variables using AspireDict
        // ctx.environmentVariables is a direct AspireDict<string, string | ReferenceExpression> field
        await ctx.environmentVariables.set("MY_CONSTANT", "hello from TypeScript");
        await ctx.environmentVariables.set("REDIS_URL", redisUrl);

        await ctx.environmentVariables.set("ANOTHER_VARIABLE", await ep.url.get());

        console.log(`    Set environment variables: MY_CONSTANT, REDIS_URL`);
    })
    .waitFor(cache)        // Union type fix: accepts RedisResource wrapper directly!
    .withReference(cache); // Now generated from internal CoreExports class!
console.log("Added API container with environment callback, waitFor, and withReference");

// Build and run - fully fluent!
console.log("\nBuilding and running...\n");
await builder.build().run();
