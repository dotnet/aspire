import { createBuilder } from './.modules/aspire.js';

/**
 * Creates the Aspire distributed application builder.
 * The builder is the central entry point for configuring services,
 * resources, and their relationships.
 * @see https://aspire.dev/get-started/app-host/
 */
const builder = await createBuilder();

/**
 * Adds the Express API backend as a Node.js application resource.
 * The `withHttpEndpoint` method exposes the port via the PORT environment variable,
 * and `withExternalHttpEndpoints` marks the endpoint as publicly accessible.
 * @see https://aspire.dev/get-started/first-app/
 */
const app = await builder
    .addNodeApp("app", "./api", "src/index.ts")
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints();

/**
 * Adds the React frontend as a Vite application resource.
 * `withServiceReference` injects the API's URL as an environment variable
 * so the frontend can proxy requests. `waitFor` ensures the API starts first.
 */
const frontend = await builder
    .addViteApp("frontend", "./frontend")
    .withServiceReference(app)
    .waitFor(app);

/**
 * Configures the API to bundle the frontend's build output into its container
 * at the "./static" path for production deployment scenarios.
 */
await app.publishWithContainerFiles(frontend, "./static");

await builder.build().run();
