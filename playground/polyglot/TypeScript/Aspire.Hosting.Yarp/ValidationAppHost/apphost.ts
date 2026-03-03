import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

await builder.addYarp("proxy")
    .withHostPort({ port: 8080 })
    .withHostHttpsPort({ port: 8443 });

await builder.build().run();
