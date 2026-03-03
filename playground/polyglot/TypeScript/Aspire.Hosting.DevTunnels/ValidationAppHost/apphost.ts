import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

await builder.addDevTunnel("devtunnel", { tunnelId: "sample-devtunnel" })
    .withAnonymousAccess();

await builder.build().run();
