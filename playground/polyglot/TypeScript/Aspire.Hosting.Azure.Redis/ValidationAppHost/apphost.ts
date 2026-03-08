import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

const keyVault = await builder.addAzureKeyVault("vault");
const cache = await builder.addAzureManagedRedis("cache");

await cache.withAccessKeyAuthentication();
await cache.withAccessKeyAuthenticationWithKeyVault(keyVault);
await cache.runAsContainer({
    configureContainer: async (container) => {
        await container.withVolume("/data");
    }
});

await builder.build().run();
