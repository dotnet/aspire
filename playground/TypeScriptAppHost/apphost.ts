// Aspire TypeScript AppHost
// For more information, see: https://learn.microsoft.com/dotnet/aspire

// Import from the generated module (created by code generation)
import {
    createBuilder,
    EnvironmentCallbackContextProxy,
    CommandLineArgsCallbackContextProxy,
} from './.modules/distributed-application.js';
import { refExpr } from './.modules/RemoteAppHostClient.js';

console.log("Aspire TypeScript AppHost starting...");

// Create the distributed application builder
const builder = await createBuilder();

// ========================================
// Test strongly-typed builder properties
// ========================================

// Test Configuration access
const config = await builder.getConfiguration();
console.log("✅ Got Configuration proxy");

// Test reading a config value using proxy's getIndexer (may be null if not set)
const aspnetEnv = await config.proxy.getIndexer("ASPNETCORE_ENVIRONMENT");
console.log(`   ASPNETCORE_ENVIRONMENT: ${aspnetEnv ?? "(not set)"}`);

// Test Environment access
const env = await builder.getEnvironment();
const envName = await env.getEnvironmentName();
const appName = await env.getApplicationName();
console.log(`✅ Got Environment: ${envName}, App: ${appName}`);

// Test environment checks (using environment name directly)
const isDev = envName === "Development";
const isProd = envName === "Production";
console.log(`   isDevelopment: ${isDev}, isProduction: ${isProd}`);

// Test ExecutionContext access
const ctx = await builder.getExecutionContext();
const isRunMode = await ctx.getIsRunMode();
const isPublishMode = await ctx.getIsPublishMode();
console.log(`✅ Got ExecutionContext: isRunMode=${isRunMode}, isPublishMode=${isPublishMode}`);

// Test convenience methods on builder (using properties directly)
console.log(`✅ builder environment: ${envName}`);
console.log(`✅ builder isRunMode: ${isRunMode}`);

// ========================================
// Conditional logic based on environment (like C# pattern)
// ========================================
if (isDev && isRunMode) {
    console.log("🔧 Running in Development + RunMode - adding dev-only configuration");
}

// ========================================
// Fluent Chaining API Demo
// ========================================
// Before (multiple awaits):
//   const redis = await builder.addContainer("myredis", "redis:latest");
//   await redis.withEnvironmentString("FOO", "BAR");
//   await redis.withEnvironmentString("BAZ", "QUX");
//
// After (single await with fluent chaining):
//   const redis = await builder.addContainer("myredis", "redis:latest")
//       .withEnvironmentString("FOO", "BAR")
//       .withEnvironmentString("BAZ", "QUX");

// Add a Redis container with fluent chaining
const redis = await builder
    .addContainer("myredis", "redis:latest")
    .withEnvironment("REDIS_VERSION", "latest")
    .withEnvironment("REDIS_MODE", "standalone");

console.log("✅ Created Redis container with fluent chaining!");

// Test error propagation - call a method that doesn't exist
try {
    await (redis as any).nonExistentMethod();
    console.log("ERROR: Should have thrown!");
} catch (e) {
    console.log("✅ Error properly propagated:", (e as Error).message);
}

// Callbacks are also chainable!
// You can chain withEnvironmentCallback and withArgs2 together
const redis2 = await builder
    .addContainer("myredis2", "redis:alpine")
    .withEnvironment("CONFIGURED", "via-fluent-chain")
    .withEnvironmentCallback(async (context: EnvironmentCallbackContextProxy) => {
        const envVars = await context.getEnvironmentVariables();
        await envVars.set("MY_CUSTOM_VAR", "Hello from TypeScript with typed proxies!");
        await envVars.set("REDIS_CONFIG", "configured-via-typescript");
        console.log("Environment variables configured via TypeScript callback!");
    })
    .withArgs2(async (context: CommandLineArgsCallbackContextProxy) => {
        const args = await context.getArgs();

        await args.add("--maxmemory");
        await args.add("256mb");
        await args.add("--maxmemory-policy");
        await args.add("allkeys-lru");

        const count = await args.count();
        console.log(`Command line args configured: ${count} arguments added!`);

        // Test list indexer
        const firstArg = await args.get(0);
        console.log(`✅ List get(0) works: "${firstArg}"`);

        await args.set(1, "512mb");
        const updatedArg = await args.get(1);
        console.log(`✅ List set(1) works: "${updatedArg}"`);

        // Test re-entrant callback
        const resource = await context.getResource();
        console.log(`✅ Re-entrant callback works! Got resource: ${resource.$type}`);
    });

console.log("✅ Created Redis2 container with fluent chaining including callbacks!");

// Test ReferenceExpression with EndpointReference
const redisEndpoint = await redis.getEndpoint("tcp");
console.log(`✅ Got endpoint: ${redisEndpoint.$type}`);

const connectionExpr = refExpr`redis://${redisEndpoint}`;
console.log(`✅ Created ReferenceExpression: ${JSON.stringify(connectionExpr)}`);

// Build and run the application
const app = builder.build();
await app.run();
