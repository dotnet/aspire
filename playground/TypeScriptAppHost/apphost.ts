// Aspire TypeScript AppHost
// For more information, see: https://learn.microsoft.com/dotnet/aspire

import { createBuilder } from '@aspire/hosting';

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
