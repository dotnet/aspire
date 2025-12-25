// Aspire TypeScript AppHost
// For more information, see: https://learn.microsoft.com/dotnet/aspire

// Import from the generated module (created by code generation)
import { createBuilder } from './.modules/distributed-application.js';

async function main() {
    console.log("Aspire TypeScript AppHost starting...");

    try {
        // Create the distributed application builder
        console.log("Creating distributed application builder...");
        const builder = await createBuilder();
        console.log("Builder created successfully!");

        // Add a Redis resource
        console.log("Adding redis...");
        const redis = await builder.addRedis("cache");
        console.log("Redis added successfully!");

        // Build and run the application
        console.log("Building and running application...");
        const app = builder.build();
        console.log("Application built!");

        await app.run();

    } catch (error) {
        console.error("Application failed:", error);
        process.exit(1);
    }
}

main();
