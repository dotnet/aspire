import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const adminPassword = await builder.addParameter("seq-admin-password", { secret: true });
const seq = await builder.addSeq("seq", adminPassword, { port: 5341 });

await seq.withDataVolume();
await seq.withDataVolume({ name: "seq-data", isReadOnly: false });
await seq.withDataBindMount("./seq-data", { isReadOnly: true });

await builder.build().run();
