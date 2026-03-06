// Aspire TypeScript AppHost - Azure SQL Validation
// Validates AspireExport coverage for Aspire.Hosting.Azure.Sql

import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();
const storage = await builder.addAzureStorage("storage");

// addAzureSqlServer — factory method on builder
const sqlServer = await builder.addAzureSqlServer("sql");

// addDatabase — child resource factory, returns AzureSqlDatabaseResource builder
const db = await sqlServer.addDatabase("mydb");

// addDatabase with explicit databaseName
const db2 = await sqlServer.addDatabase("inventory", { databaseName: "inventorydb" });

// withDefaultAzureSku — simple property setter on database resource
await db2.withDefaultAzureSku();

// runAsContainer — run Azure SQL locally in a container
await sqlServer.runAsContainer({ configureContainer: async _ => {} });

// withAdminDeploymentScriptStorage — configure deployment script storage
await sqlServer.withAdminDeploymentScriptStorage(storage);

// Fluent chaining — addDatabase + withDefaultAzureSku
const db3 = await sqlServer.addDatabase("analytics").withDefaultAzureSku();

async function validateAzureSqlServerMembers()
{
    const hostName = await sqlServer.hostName.get();
    const port = await sqlServer.port.get();
    const uriExpression = await sqlServer.uriExpression.get();
    const connectionStringExpression = await sqlServer.connectionStringExpression.get();
    const jdbcConnectionString = await sqlServer.jdbcConnectionString.get();
    const isContainer = await sqlServer.isContainer.get();
    const databaseCount = await sqlServer.databases.count();
    const hasMyDb = await sqlServer.databases.containsKey("mydb");

    void [hostName, port, uriExpression, connectionStringExpression, jdbcConnectionString, isContainer, databaseCount, hasMyDb];
}

async function validateAzureSqlDatabaseMembers()
{
    const parent = await db.parent.get();
    const connectionStringExpression = await db.connectionStringExpression.get();
    const databaseName = await db.databaseName.get();
    const isContainer = await db.isContainer.get();
    const uriExpression = await db.uriExpression.get();
    const jdbcConnectionString = await db.jdbcConnectionString.get();

    void [parent, connectionStringExpression, databaseName, isContainer, uriExpression, jdbcConnectionString];
}

void [storage, sqlServer, db, db2, db3, validateAzureSqlServerMembers, validateAzureSqlDatabaseMembers];

await builder.build().run();
