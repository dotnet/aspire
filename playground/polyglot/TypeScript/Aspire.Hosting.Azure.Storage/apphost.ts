// Aspire.Hosting.Azure.Storage - AspireExport Validation AppHost
// This validates that all [AspireExport] attributed methods work from TypeScript.
// Run with: aspire run (with ASPIRE_REPO_ROOT set)

import { createBuilder, AzureStorageEmulatorResource } from './.modules/aspire.js';

const builder = await createBuilder();

// Test: addAzureStorage - creates Azure Storage resource
const storage = await builder.addAzureStorage("storage");

// Test: runAsEmulator - configures to use Azurite emulator
// This also tests the configureContainer callback
await storage.runAsEmulator({
    configureContainer: async (emulator: AzureStorageEmulatorResource) => {
        // Test: withDataVolume on emulator container
        await emulator.withDataVolume();
    }
});

// Test: addBlobs - adds blob storage child resource
const blobs = await storage.addBlobs("blobs");

// Test: addQueues - adds queue storage child resource  
const queues = await storage.addQueues("queues");

// Test: addTables - adds table storage child resource
const tables = await storage.addTables("tables");

// Test: addDataLake - adds Data Lake storage child resource
const dataLake = await storage.addDataLake("datalake");

// Test: addBlobContainer - adds a blob container
const blobContainer = await blobs.addBlobContainer("container1");

// Test: addQueue - adds a queue 
const queue = await queues.addQueue("myqueue");

// Test: addDataLakeFileSystem - adds a Data Lake file system
const fileSystem = await dataLake.addDataLakeFileSystem("filesystem");

// Test: Second storage with emulator using withDataBindMount
const storage2 = await builder.addAzureStorage("storage2");
await storage2.runAsEmulator({
    configureContainer: async (emulator: AzureStorageEmulatorResource) => {
        // Test: withDataBindMount on emulator container
        await emulator.withDataBindMount({ path: "./data/storage2" });
    }
});

// Build and run
await builder.build().run();
