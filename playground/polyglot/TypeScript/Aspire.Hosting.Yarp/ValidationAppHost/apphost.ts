import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const staticFilesSource = await builder.addContainer("static-files-source", "nginx");

await builder.addYarp("proxy")
    .withHostPort({ port: 8080 })
    .withHostHttpsPort({ port: 8443 })
    .publishWithStaticFiles(staticFilesSource);

await builder.build().run();
