// Aspire TypeScript AppHost
// For more information, see: https://learn.microsoft.com/dotnet/aspire

// Import from the generated module (created by code generation)
import {
    createBuilder,
    EnvironmentCallbackContextProxy,
    CommandLineArgsCallbackContextProxy,
    EndpointReferenceProxy,
    ConfigurationProxy,
    HostEnvironmentProxy,
    ExecutionContextProxy
} from './.modules/distributed-application.js';
import { ListProxy, refExpr } from './.modules/RemoteAppHostClient.js';

async function main() {
    console.log("Aspire TypeScript AppHost starting...");

    try {
        // Create the distributed application builder
        const builder = await createBuilder();

        // ========================================
        // Test strongly-typed builder properties
        // ========================================

        // Test Configuration access
        const config = await builder.getConfiguration();
        console.log("✅ Got Configuration proxy");

        // Test reading a config value (may be null if not set)
        const aspnetEnv = await config.get("ASPNETCORE_ENVIRONMENT");
        console.log(`   ASPNETCORE_ENVIRONMENT: ${aspnetEnv ?? "(not set)"}`);

        // Test Environment access
        const env = await builder.getEnvironment();
        const envName = await env.getEnvironmentName();
        const appName = await env.getApplicationName();
        console.log(`✅ Got Environment: ${envName}, App: ${appName}`);

        // Test environment checks
        const isDev = await env.isDevelopment();
        const isProd = await env.isProduction();
        console.log(`   isDevelopment: ${isDev}, isProduction: ${isProd}`);

        // Test ExecutionContext access
        const ctx = await builder.getExecutionContext();
        const isRunMode = await ctx.isRunMode();
        const isPublishMode = await ctx.isPublishMode();
        console.log(`✅ Got ExecutionContext: isRunMode=${isRunMode}, isPublishMode=${isPublishMode}`);

        // Test convenience methods on builder
        console.log(`✅ builder.isDevelopment(): ${await builder.isDevelopment()}`);
        console.log(`✅ builder.isRunMode(): ${await builder.isRunMode()}`);

        // ========================================
        // Conditional logic based on environment (like C# pattern)
        // ========================================
        if (await builder.isDevelopment() && await builder.isRunMode()) {
            console.log("🔧 Running in Development + RunMode - adding dev-only configuration");
        }

        // Add a Redis container
        const redis = await builder.addContainer("myredis", "redis:latest");

        // Test error propagation - call a method that doesn't exist
        // This should throw an error that propagates to our catch block
        try {
            await (redis as any).nonExistentMethod();
            console.log("ERROR: Should have thrown!");
        } catch (e) {
            console.log("✅ Error properly propagated:", (e as Error).message);
        }

        // Use WithEnvironment callback to set custom environment variables
        // The callback receives a typed EnvironmentCallbackContextProxy with property accessors
        await redis.withEnvironmentCallback(async (context: EnvironmentCallbackContextProxy) => {
            // Get the EnvironmentVariables dictionary using the typed accessor
            const envVars = await context.getEnvironmentVariables();

            // Set environment variables - these will be passed to the container
            await envVars.set("MY_CUSTOM_VAR", "Hello from TypeScript with typed proxies!");
            await envVars.set("REDIS_CONFIG", "configured-via-typescript");

            console.log("Environment variables configured via TypeScript callback!");
        });

        // Use WithArgs callback to add command line arguments
        // The callback receives a typed CommandLineArgsCallbackContextProxy
        // withArgs2 is the callback overload (withArgs takes string[])
        await redis.withArgs2(async (context: CommandLineArgsCallbackContextProxy) => {
            // Get the Args list using the typed accessor - returns ListProxy
            const args = await context.getArgs();

            // Add command line arguments to the container
            await args.add("--maxmemory");
            await args.add("256mb");
            await args.add("--maxmemory-policy");
            await args.add("allkeys-lru");

            // Get the count of args
            const count = await args.count();
            console.log(`Command line args configured: ${count} arguments added!`);

            // Test list indexer - get and set by index
            const firstArg = await args.get(0);
            console.log(`✅ List get(0) works: "${firstArg}"`);

            // Test set by index
            await args.set(1, "512mb");
            const updatedArg = await args.get(1);
            console.log(`✅ List set(1) works: "${updatedArg}"`);

            // TEST RE-ENTRANT CALLBACK: During this callback, call back to .NET
            // This tests that callback execution doesn't deadlock when calling .NET
            // Previously this would deadlock due to blocking .GetAwaiter().GetResult()
            const resource = await context.getResource();
            console.log(`✅ Re-entrant callback works! Got resource: ${resource.$type}`);
        });

        // Test ReferenceExpression with EndpointReference
        // Get an endpoint from redis and use it in a reference expression
        // getEndpoint returns an EndpointReferenceProxy that wraps a DotNetProxy
        const redisEndpoint = await redis.getEndpoint("tcp");
        console.log(`✅ Got endpoint: ${redisEndpoint.$type}`);

        // Create a ReferenceExpression using the refExpr template literal
        // refExpr accepts proxy wrappers directly - it extracts the underlying proxy
        const connectionExpr = refExpr`redis://${redisEndpoint}`;
        console.log(`✅ Created ReferenceExpression: ${JSON.stringify(connectionExpr)}`);

        // Build and run the application
        const app = builder.build();
        await app.run();

    } catch (error) {
        console.error("Application failed:", error);
        process.exit(1);
    }
}

main();
