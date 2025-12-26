// Aspire TypeScript AppHost
// For more information, see: https://learn.microsoft.com/dotnet/aspire

// Import from the generated module (created by code generation)
import { createBuilder, EnvironmentCallbackContextProxy } from './.modules/distributed-application.js';

async function main() {
    console.log("Aspire TypeScript AppHost starting...");

    try {
        // Create the distributed application builder
        const builder = await createBuilder();

        // Add a Redis container
        const redis = await builder.addContainer("myredis", "redis:latest");

        // Use WithEnvironment callback to set custom environment variables
        // The callback now receives a typed EnvironmentCallbackContextProxy with property accessors
        await redis.withEnvironment(async (context: EnvironmentCallbackContextProxy) => {
            // Get the EnvironmentVariables dictionary using the typed accessor
            const envVars = await context.getEnvironmentVariables();

            // Set environment variables - these will be passed to the container
            await envVars.set("MY_CUSTOM_VAR", "Hello from TypeScript with typed proxies!");
            await envVars.set("REDIS_CONFIG", "configured-via-typescript");

            console.log("Environment variables configured via TypeScript callback!");
        });

        // Build and run the application
        const app = builder.build();
        await app.run();

    } catch (error) {
        console.error("Application failed:", error);
        process.exit(1);
    }
}

main();
