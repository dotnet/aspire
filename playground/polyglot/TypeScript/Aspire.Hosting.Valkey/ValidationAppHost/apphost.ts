import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();
const password = await builder.addParameter('valkey-password', { secret: true });
const valkey = await builder.addValkey('cache', { port: 6380, password });

await valkey
    .withDataVolume({ name: 'valkey-data' })
    .withDataBindMount('.', { isReadOnly: true })
    .withPersistence({ interval: 100000000, keysChangedThreshold: 1 });

await builder.build().run();
