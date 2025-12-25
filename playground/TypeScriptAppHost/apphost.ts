// Aspire TypeScript AppHost
// For more information, see: https://learn.microsoft.com/dotnet/aspire

import { createBuilder, registerCallback } from '@aspire/hosting';

async function main() {
    console.log("Aspire TypeScript AppHost starting...");

    try {
        // Create the distributed application builder
        console.log("Creating distributed application builder...");
        const builder = await createBuilder();
        console.log("Builder created successfully!");

        // Add a container resource (using redis image for testing)
        console.log("Adding redis container...");
        const redis = await builder.addContainer("cache", "redis");
        console.log("Container added successfully!");

        // Test callback: Add environment variable with static value
        console.log("Adding static environment variable...");
        await redis.withEnvironment("STATIC_VAR", "hello-from-typescript");
        console.log("Static environment variable added!");

        // Test callback: Add environment variable with callback
        console.log("Adding callback-based environment variable...");
        await redis.withEnvironment((context) => {
            console.log(">>> CALLBACK INVOKED FROM .NET! <<<");
            console.log("Context received:", JSON.stringify(context));
            context.environmentVariables["DYNAMIC_VAR"] = `dynamic-value-${Date.now()}`;
            console.log(">>> CALLBACK COMPLETED <<<");
        });
        console.log("Callback-based environment variable added!");

        // Build and run the application
        console.log("Building and running application...");
        const app = await builder.build();
        console.log("Application started!");

        await app.run();

    } catch (error) {
        console.error("Application failed:", error);
        process.exit(1);
    }
}

main();
