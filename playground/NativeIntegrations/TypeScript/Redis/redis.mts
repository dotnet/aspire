// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { fileURLToPath } from 'node:url';
import { AspireExport, defineIntegration } from './base.mjs';
import type { Composition, DeclaredResource, DeclaredValue, ResourceLifecycleScope } from './aspire.mjs';
import { IntegrationResource } from './integration-host.mjs';
import { nativeTypes } from './native-types.mjs';

const resourceType = { typeId: 'example.ts.redis/RedisResource', category: 'Handle' } as const;
const text = { typeId: 'string', category: 'Primitive' } as const;

/** Redis-owned API over a generic native container and symbolic connection. */
export class RedisResource extends IntegrationResource {
    private lifetime?: AbortController;
    constructor(readonly nativeDeclaration: DeclaredResource, readonly connection: DeclaredValue) {
        super(nativeDeclaration, resourceType.typeId);
    }
    initialize(): void { this.lifetime = new AbortController(); }
    override async dispose(): Promise<void> {
        this.lifetime?.abort();
        this.lifetime = undefined;
    }
}

export const addRedis = AspireExport({
    id: 'example.ts.redis/addRedis', method: 'addRedis',
    description: 'Declares authenticated run-only Redis and native protocol health.',
    projection: {
        parameters: [{ name: 'composition', type: nativeTypes.Composition }, { name: 'name', type: text },
            { name: 'password', type: nativeTypes.DeclaredValue }],
        returnType: resourceType
    }
}, async ({ composition, name, password }: { composition: Composition; name: string; password: DeclaredValue }) => {
    const resource = await composition.addContainer(name, 'redis:7.4-alpine');
    await resource.withEntrypoint('/bin/sh');
    await resource.withEnvironment('REDIS_PASSWORD', password);
    // Environment expansion requires explicit shell execution and quoting.
    for (const argument of ['-c', 'exec redis-server --requirepass "$REDIS_PASSWORD" --appendonly yes --appendfsync always --dir /data']) {
        await resource.withArgument(await composition.createLiteralValue(argument));
    }
    await resource.withEndpoint({ name: 'tcp', protocol: 'TCP', uriScheme: 'redis',
        targetPort: 6379, hostPort: 0, environmentVariable: '' });
    await resource.setSymbolicConfiguration('redis.health.host', await composition.createEndpointValue(resource, 'tcp', 'host'));
    await resource.setSymbolicConfiguration('redis.health.port', await composition.createEndpointValue(resource, 'tcp', 'port'));
    await resource.setSymbolicConfiguration('redis.health.password', password);
    await resource.withLifecycleExecutable('health', {
        executablePath: process.execPath, workingDirectory: fileURLToPath(new URL('.', import.meta.url)),
        arguments: [fileURLToPath(new URL('./redis-provider.mjs', import.meta.url)), '--redis-health'],
        environment: [], endpoints: []
    }, { environment: [
        { name: 'REDIS_HOST', configurationKey: 'redis.health.host' },
        { name: 'REDIS_PORT', configurationKey: 'redis.health.port' },
        { name: 'REDIS_PASSWORD', configurationKey: 'redis.health.password' }
    ], arguments: [] });
    const endpoint = await composition.createEndpointValue(resource, 'tcp', 'hostAndPort');
    const prefix = await endpoint.concatValue(await composition.createLiteralValue(',password='));
    return new RedisResource(resource, await prefix.concatValue(password));
});

export const withDataVolume = AspireExport({
    id: 'example.ts.redis/withDataVolume', method: 'withDataVolume',
    description: 'Mounts retained Redis AOF data at /data.',
    projection: { targetTypeId: resourceType.typeId, targetParameterName: 'context',
        parameters: [{ name: 'context', type: resourceType }, { name: 'name', type: text }], returnType: resourceType }
}, async ({ context, name }: { context: RedisResource; name: string }) => {
    await context.nativeDeclaration.withVolume(name, '/data', false);
    return context;
});

export const withReference = AspireExport({
    id: 'example.ts.redis/withReference', method: 'withReference',
    description: 'References the late-bound Redis connection from a host executable.',
    projection: { targetTypeId: resourceType.typeId, targetParameterName: 'context',
        parameters: [{ name: 'context', type: resourceType }, { name: 'consumer', type: nativeTypes.DeclaredResource },
            { name: 'environmentName', type: text }], returnType: nativeTypes.DeclaredResource }
}, async ({ context, consumer, environmentName }: { context: RedisResource; consumer: DeclaredResource; environmentName: string }) => {
    await consumer.withReference(environmentName, context.connection);
    return consumer;
});

const lifecycle = (phase: 'initialize' | 'allocated' | 'ready' | 'retiring', method: string,
    action: (context: RedisResource, scope: ResourceLifecycleScope, signal: AbortSignal) => Promise<void>) =>
    AspireExport({
        id: `example.ts.redis/${method}`, method, description: `Redis-owned ${phase} lifecycle.`,
        projection: {
            targetTypeId: resourceType.typeId, targetParameterName: 'context', lifecyclePhase: phase,
            parameters: [{ name: 'context', type: resourceType }, { name: 'scope', type: nativeTypes.ResourceLifecycleScope }],
            returnType: { typeId: 'void', category: 'Primitive' }
        }
    }, async ({ context, scope, signal }: { context: RedisResource; scope: ResourceLifecycleScope; signal: AbortSignal }) => {
        signal.throwIfAborted();
        await action(context, scope, signal);
    });

const initialize = lifecycle('initialize', 'redisInitialize', async (context, scope) => {
    context.initialize();
    await scope.writeLifecycleLog('stdout', 'Redis integration initialized.');
});
const allocated = lifecycle('allocated', 'redisAllocated', async (_context, scope) => {
    await scope.writeLifecycleLog('stdout', 'Redis endpoint allocated.');
});
const ready = lifecycle('ready', 'redisReady', async (_context, scope) => {
    await scope.writeLifecycleLog('stdout', 'Redis authenticated and ready.');
});
const retiring = lifecycle('retiring', 'redisRetiring', async (_context, scope) => {
    if (!await scope.runLifecycleExecutable({
        executablePath: process.execPath, workingDirectory: fileURLToPath(new URL('.', import.meta.url)),
        arguments: [fileURLToPath(new URL('./redis-provider.mjs', import.meta.url)), '--redis-save'],
        environment: [], endpoints: []
    }, { environment: [
        { name: 'REDIS_HOST', configurationKey: 'redis.health.host' },
        { name: 'REDIS_PORT', configurationKey: 'redis.health.port' },
        { name: 'REDIS_PASSWORD', configurationKey: 'redis.health.password' }
    ], arguments: [] })) {
        throw new Error('Redis final authenticated snapshot failed.');
    }
});

export const integration = defineIntegration({ name: 'Redis',
    capabilities: [addRedis, withDataVolume, withReference, initialize, allocated, ready, retiring] });
