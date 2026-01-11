// Aspire TypeScript AppHost - E2E Demo with PostgreSQL and Express
// This demonstrates compute, databases, and references working together.
// Run with: aspire run

import { createBuilder, refExpr, EnvironmentCallbackContext } from './.modules/aspire.js';

console.log("Aspire TypeScript AppHost starting...\n");

// Create the distributed application builder
const builder = await createBuilder();

var ec = await builder.executionContext.get();

console.log(`isRunMode: ${await ec.isRunMode.get()}`);
console.log(`isPublishMode: ${await ec.isPublishMode.get()}`);

var dir = await builder.appHostDirectory.get();
console.log(`AppHost directory: ${dir}`);

// Add PostgreSQL server and database
const postgres = await builder.addPostgres("postgres");
const db = await postgres.addDatabase("db");

console.log("Added PostgreSQL server with database 'db'");

// Add Express API that connects to PostgreSQL (uses npm run dev with tsx)
const api = await builder
    .addNodeApp("api", "./express-api", "src/server.ts")
    .withRunScript("dev")
    .withHttpEndpoint(undefined, undefined, undefined, "PORT")
    .withReference(db)
    .waitFor(db);

console.log("Added Express API with reference to PostgreSQL database");

// Also keep Redis as an example of another service
const cache = await builder
    .addRedis("cache")
    .withEnvironment("CUSTOM_ENV", "value");

console.log("Added Redis cache");

// Add Vite frontend that connects to the API (using withServiceReference for endpoints)
await builder
    .addViteApp("frontend", "./vite-frontend")
    .withServiceReference(api)
    .waitFor(api);

console.log("Added Vite frontend with reference to API");

await builder.build().run();
