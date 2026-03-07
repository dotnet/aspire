import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const app = await builder
    .addNodeApp("app", "./api", "src/index.ts")
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints();

const frontend = await builder
    .addViteApp("frontend", "./frontend")
    .withServiceReference(app)
    .waitFor(app);

await app.publishWithContainerFiles(frontend, "./static");

await builder.build().run();
