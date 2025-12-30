// Aspire TypeScript AppHost
// For more information, see: https://learn.microsoft.com/dotnet/aspire

// Import from the generated module (created by code generation)
import {
    createBuilder,
    EnvironmentCallbackContextProxy,
} from './.modules/distributed-application.js';
import { refExpr } from './.modules/RemoteAppHostClient.js';

console.log("Aspire TypeScript AppHost starting...");

// Create the distributed application builder
const builder = await createBuilder();

// ========================================
// Test strongly-typed builder properties with NEW thenable API!
// ========================================

// NEW: Fluent chaining through getEnvironment() -> getEnvironmentName()
// Instead of:
//   const env = await builder.getEnvironment();
//   const envName = await env.getEnvironmentName();
// You can now write:
const envName = await builder.getEnvironment().getEnvironmentName();
const appName = await builder.getEnvironment().getApplicationName();
console.log(`✅ Got Environment: ${envName}, App: ${appName} (via fluent chain!)`);

// Test environment checks
const isDev = envName === "Development";
const isProd = envName === "Production";
console.log(`   isDevelopment: ${isDev}, isProduction: ${isProd}`);

// NEW: Fluent chaining through getExecutionContext() -> properties
const isRunMode = await builder.getExecutionContext().getIsRunMode();
const isPublishMode = await builder.getExecutionContext().getIsPublishMode();
console.log(`✅ Got ExecutionContext: isRunMode=${isRunMode}, isPublishMode=${isPublishMode} (via fluent chain!)`);

// Configuration still works the same way (for indexer access)
const config = await builder.getConfiguration();
console.log("✅ Got Configuration proxy");
const aspnetEnv = await config.proxy.getIndexer("ASPNETCORE_ENVIRONMENT");
console.log(`   ASPNETCORE_ENVIRONMENT: ${aspnetEnv ?? "(not set)"}`);

// You can also get a full proxy if needed for multiple operations
const env = await builder.getEnvironment();
console.log(`✅ Environment proxy type: ${env.proxy.$type}`);

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
// You can chain withEnvironmentCallback together with other methods
const redis2 = await builder
    .addContainer("myredis2", "redis:alpine")
    .withEnvironment("CONFIGURED", "via-fluent-chain")
    .withEnvironmentCallback(async (context: EnvironmentCallbackContextProxy) => {
        const envVars = await context.getEnvironmentVariables();
        await envVars.set("MY_CUSTOM_VAR", "Hello from TypeScript with typed proxies!");
        await envVars.set("REDIS_CONFIG", "configured-via-typescript");
        console.log("Environment variables configured via TypeScript callback!");

        // Test re-entrant callback - get the resource from inside the callback
        const resource = await context.getResource();
        console.log(`✅ Re-entrant callback works! Got resource: ${resource.proxy.$type}`);
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
