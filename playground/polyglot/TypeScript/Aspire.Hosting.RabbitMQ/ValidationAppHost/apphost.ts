import { createBuilder, ContainerLifetime } from './.modules/aspire.js';

const builder = await createBuilder();

const rabbitmq = await builder.addRabbitMQ("messaging");
await rabbitmq.withDataVolume();
await rabbitmq.withManagementPlugin();

const rabbitmq2 = await builder
    .addRabbitMQ("messaging2")
    .withLifetime(ContainerLifetime.Persistent)
    .withDataVolume()
    .withManagementPluginWithPort({ port: 15673 });

await builder.build().run();