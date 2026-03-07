import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();
await builder.build().run();
