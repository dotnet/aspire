import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();
await builder.addYarp('proxy');
await builder.build().run();
