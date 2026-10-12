// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dirname } from 'node:path';
import { connectNativeAppHost } from './native-client.mjs';
import { IntegrationClient } from './provider-client.mjs';

const native = await connectNativeAppHost({ endpoint: { host: '127.0.0.1', port: Number(process.argv[2]) },
    authenticationToken: process.env.ASPIRE_NATIVE_RPC_TOKEN!, authenticationTimeoutMilliseconds: 15000 });
const provider = await native.activateProvider('example.ts.redis', process.execPath, [process.argv[3]]);
try {
    const integration = new IntegrationClient(native.client);
    const workspace = await native.server.createApplicationWorkspace();
    let started = false;
    for (const operation of ['write', 'read']) {
        const application = await workspace.beginRevision();
        const password = await application.addSecretParameter('cache-password',
            operation === 'write' ? 'real-authoring-password' : 'real-authoring-password-rotated');
        const cache = await integration.addRedis(application, 'cache', password);
        await cache.withDataVolume(process.argv[4]);
        const consumer = await application.addExecutable('consumer', process.execPath, dirname(process.argv[3]));
        for (const argument of [process.argv[3], '--redis-consume', operation]) {
            await consumer.withArgument(await application.createLiteralValue(argument));
        }
        await cache.withReference(consumer, 'ConnectionStrings__cache');
        await workspace.commitRevision(application);
        const running = started ? await workspace.getApplicationExecution() : await application.startApplication();
        started = true;
        await running.waitApplicationReady(60000);
        if (!await running.waitResourceCompletion(consumer, 60000)) {
            throw new Error('The real Redis consumer failed.');
        }
    }
    await workspace.retireApplicationWorkspace();
    console.log('redis-persistence-ok');
} finally {
    await provider.close();
    await native.client.disconnect();
}
