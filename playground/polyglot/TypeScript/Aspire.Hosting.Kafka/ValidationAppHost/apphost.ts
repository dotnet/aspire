// Aspire TypeScript AppHost — Kafka integration validation
// Exercises all [AspireExport] methods for Aspire.Hosting.Kafka

import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

// addKafka — factory method with optional port
const kafka = builder.addKafka("broker");

// withKafkaUI — adds Kafka UI management container with callback
const kafkaWithUi = kafka.withKafkaUI({
    configureContainer: async (ui) => {
        // withHostPort — sets the host port for Kafka UI
        await ui.withHostPort({ port: 9000 });
    },
    containerName: "my-kafka-ui",
});

// withDataVolume — adds a data volume
kafkaWithUi.withDataVolume();

// withDataBindMount — adds a data bind mount
const kafka2 = builder.addKafka("broker2", { port: 19092 });
kafka2.withDataBindMount("/tmp/kafka-data");

await builder.build().run();