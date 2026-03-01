// Aspire TypeScript AppHost - Azure SQL Validation
// Validates AspireExport coverage for Aspire.Hosting.Azure.Sql

import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

// addAzureSqlServer — factory method on builder
const sqlServer = await builder.addAzureSqlServer("sql");

// addDatabase — child resource factory, returns AzureSqlDatabaseResource builder
const db = await sqlServer.addDatabase("mydb");

// addDatabase with explicit databaseName
const db2 = await sqlServer.addDatabase("inventory", { databaseName: "inventorydb" });

// withDefaultAzureSku — simple property setter on database resource
await db2.withDefaultAzureSku();

// runAsContainer — run Azure SQL locally in a container
await sqlServer.runAsContainer();

// Fluent chaining — addDatabase + withDefaultAzureSku
const db3 = await sqlServer.addDatabase("analytics").withDefaultAzureSku();

await builder.build().run();