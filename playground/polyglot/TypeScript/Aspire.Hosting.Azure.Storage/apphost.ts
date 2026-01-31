// Aspire.Hosting.Azure.Storage - AspireExport Validation AppHost
// This validates that all [AspireExport] attributed methods work from TypeScript.
// Run with: aspire run (with ASPIRE_REPO_ROOT set)

import { createBuilder, AzureStorageEmulatorResource } from './.modules/aspire.js';

const builder = await createBuilder();

// Test: addAzureStorage - creates Azure Storage resource
const storage = await builder.addAzureStorage("storage");

// Test the configureContainer callback
// await storage.runAsEmulator({
//     configureContainer: async (emulator: AzureStorageEmulatorResource) => {
//         // Test: withDataVolume on emulator container
//         await emulator.withDataVolume();
//     }
// });

await storage.runAsEmulator();

// Test: addBlobs - adds blob storage child resource
const blobs = await storage.addBlobs("blobs");

// Test: addQueues - adds queue storage child resource  
const queues = await storage.addQueues("queues");

// Test: addTables - adds table storage child resource
const tables = await storage.addTables("tables");

// Test: addBlobContainer - adds a blob container (on AzureStorageResource, not AzureBlobStorageResource)
const blobContainer = await storage.addBlobContainer("container1");

// Test: addQueue - adds a queue (on AzureStorageResource, not AzureQueueStorageResource)
const queue = await storage.addQueue("myqueue");

// Data Lake is not supported in emulator, so we skip testing addDataLake here

// Build and run
await builder.build().run();
