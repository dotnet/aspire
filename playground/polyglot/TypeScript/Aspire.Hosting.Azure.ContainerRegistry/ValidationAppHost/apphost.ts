import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

await builder.addAzureContainerRegistry("containerregistry")
    .withPurgeTask("0 1 * * *", {
        filter: "samples:*",
        ago: 7,
        keep: 5,
        taskName: "purge-samples"
    });

await builder.build().run();
