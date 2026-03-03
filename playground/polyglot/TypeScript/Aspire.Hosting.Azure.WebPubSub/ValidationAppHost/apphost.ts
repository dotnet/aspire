import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

await builder.addAzureWebPubSub("webpubsub");

await builder.build().run();
