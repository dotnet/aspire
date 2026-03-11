import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();
const qdrant = await builder.addQdrant('qdrant');
await qdrant.withDataVolume({ name: 'qdrant-data' }).withDataBindMount('.', { isReadOnly: true });
await builder.build().run();
