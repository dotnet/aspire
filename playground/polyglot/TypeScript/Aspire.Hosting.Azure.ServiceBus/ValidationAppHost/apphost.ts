// Aspire TypeScript AppHost — Azure Service Bus validation
// Exercises all exported methods from Aspire.Hosting.Azure.ServiceBus

import {
    createBuilder,
    AzureServiceBusFilterType,
    type AzureServiceBusRule,
    type AzureServiceBusCorrelationFilter,
} from './.modules/aspire.js';

const builder = await createBuilder();

// 1. addAzureServiceBus — creates the top-level Service Bus resource
const serviceBus = await builder.addAzureServiceBus("messaging");

// 2. runAsEmulator — with configureContainer callback exercising emulator methods
const emulatorBus = await builder
    .addAzureServiceBus("messaging-emulator")
    .runAsEmulator({
        configureContainer: async (emulator) => {
            // withConfigurationFile
            await emulator.withConfigurationFile("./servicebus-config.json");
            // withHostPort
            await emulator.withHostPort({ port: 5672 });
        },
    });

// 3. addServiceBusQueue — adds a queue to the service bus
//    Note: codegen returns AzureServiceBusResource (parent type) instead of
//    AzureServiceBusQueueResource. This is a known codegen limitation.
const busWithQueue = await serviceBus.addServiceBusQueue("orders", {
    queueName: "orders-queue",
});

// 4. addServiceBusTopic — adds a topic to the service bus
const busWithTopic = await serviceBus.addServiceBusTopic("events", {
    topicName: "events-topic",
});

// 5. withRoleAssignments — on the parent resource
await serviceBus.withRoleAssignments(serviceBus, [
    "AzureServiceBusDataOwner",
    "AzureServiceBusDataSender",
]);

// 6. Validate DTO types compile correctly
const filter: AzureServiceBusCorrelationFilter = {
    correlationId: "order-123",
    subject: "OrderCreated",
    contentType: "application/json",
};

const rule: AzureServiceBusRule = {
    name: "order-filter",
    filterType: AzureServiceBusFilterType.CorrelationFilter,
    correlationFilter: filter,
};

// Verify the enum values are accessible
const sqlFilter = AzureServiceBusFilterType.SqlFilter;
const correlationFilter = AzureServiceBusFilterType.CorrelationFilter;

await builder.build().run();