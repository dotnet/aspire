import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const api = await builder
    .addNodeApp("api", "./api", "src/index.ts")
    .withHttpEndpoint({ env: "PORT" })
    .withExternalHttpEndpoints();

const frontend = await builder
    .addViteApp("frontend", "./frontend")
    .withServiceReference(api)
    .waitFor(api);

api.publishWithContainerFiles(frontend, "./static");

await builder.build().run();
