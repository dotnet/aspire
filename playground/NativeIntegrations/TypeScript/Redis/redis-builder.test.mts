// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import assert from 'node:assert/strict';
import { createServer } from 'node:net';
import { setImmediate } from 'node:timers/promises';
import { test } from 'node:test';
import { createBuilder } from './redis-builder.mjs';
import { RedisContainerResource } from './redis-authoring.mjs';
import { ExecutableResource, NativeApplication } from './native-builder.mjs';

test('familiar awaited fluent composition and build perform no RPC', async () => {
    let connections = 0;
    const listener = createServer(socket => { connections++; socket.destroy(); });
    await new Promise<void>(resolve => listener.listen(0, '127.0.0.1', resolve));
    try {
        const address = listener.address();
        assert(address && typeof address !== 'string');
        const builder = await createBuilder([`--native-endpoint=127.0.0.1:${address.port}`]);
        const cache = await builder.addRedis('cache').withDataVolume();
        const consumer = await builder.addExecutable('consumer', 'unused-executable', '.').withReference(cache).waitFor(cache);
        const application = builder.build();
        assert(cache.resource instanceof RedisContainerResource);
        assert(consumer.resource instanceof ExecutableResource);
        assert(application instanceof NativeApplication);
        assert.equal(cache.applicationBuilder, builder);
        assert.equal(consumer.applicationBuilder, builder);
        assert.equal('then' in cache, false);
        assert.deepEqual(builder.resources.map(resource => resource.name), ['cache-password', 'cache', 'consumer']);
        await setImmediate();
        assert.equal(connections, 0);
        assert.throws(() => cache.withDataVolume('after-build'), /sealed/);
        assert.throws(() => builder.addRedis('after-build'), /sealed/);
    } finally {
        await new Promise<void>((resolve, reject) => listener.close(error => error ? reject(error) : resolve()));
    }
});

test('cross-plan references and unsupported publishing fail explicitly', async () => {
    const first = await createBuilder([]);
    const second = await createBuilder([]);
    const cache = first.addRedis('cache');
    const consumer = second.addExecutable('consumer', 'unused-executable', '.');
    assert.throws(() => consumer.withReference(cache), /another authoring plan/);
    assert.throws(() => consumer.waitFor(cache), /another authoring plan/);
    await assert.rejects(createBuilder(['--publish=true']), /not supported/);
});

test('pre-cancelled run acquires no native authority and disposal joins once', async () => {
    const builder = await createBuilder([]);
    const application = builder.build();
    await application.run(AbortSignal.abort());
    const first = application.dispose(), second = application.dispose();
    assert.equal(first, second);
    await first;
    await assert.rejects(application.start());
});
