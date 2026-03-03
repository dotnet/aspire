import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const githubModel = builder.addGitHubModel('validation-model');
githubModel.withApiKey({ secret: true });
await builder.build().run();
