import { createBuilder } from './.aspire/modules/aspire.mjs';

const builder = await createBuilder();

const cache = await builder.addRedis("cache");

const postgres = await builder.addPostgres("postgres");
const db = await postgres.addDatabase("db")
    .withCommand("reset-data", "Reset data", async () => ({ success: true }),
        { commandOptions: { confirmationMessage: "Reset the orders?" } });

const catalog = await builder
    .addNodeApp("catalog", "./catalog", "src/index.ts")
    .withHttpEndpoint({ env: "PORT" })
    .withHttpHealthCheck({ path: "/health" })

const api = await builder
    .addProject("api", "./Api/Api.csproj")
    .withHttpHealthCheck({ path: "/health" })
    .withReference(db)
    .withReference(catalog)
    .waitFor(db);

await builder
    .addViteApp("frontend", "./frontend")
    .withReference(api)
    .withExternalHttpEndpoints()
    .waitFor(api)
    .publishAsStaticWebsite({ apiPath: "/api", apiTarget: api });

await builder.build().run();
