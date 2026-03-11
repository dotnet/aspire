import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();
const rootPassword = await builder.addParameter('mysql-root-password', { secret: true });
const mysql = await builder.addMySql('mysql', { password: rootPassword, port: 3306 });

await mysql
    .withPassword(rootPassword)
    .withDataVolume({ name: 'mysql-data' })
    .withDataBindMount('.', { isReadOnly: true })
    .withInitFiles('.');

await mysql.withPhpMyAdmin({
    containerName: 'phpmyadmin',
    configureContainer: async (container) => {
        await container.withHostPort({ port: 8080 });
    }
});

const db = await mysql.addDatabase('appdb', { databaseName: 'appdb' });
await db.withCreationScript('CREATE DATABASE IF NOT EXISTS appdb;');

await builder.build().run();
