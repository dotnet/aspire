import { createBuilder } from './.modules/aspire.js';

const builder = await createBuilder();

// addRedis — full overload with port and password parameter
const password = await builder.addParameter("redis-password", { secret: true });
const cache = builder.addRedis("cache", { password: password });

// addRedisWithPort — overload with explicit port
const cache2 = builder.addRedisWithPort("cache2", { port: 6380 });

// withDataVolume + withPersistence — fluent chaining on RedisResource
cache.withDataVolume({ name: "redis-data" })
     .withPersistence({ interval: 600000000, keysChangedThreshold: 5 });

// withDataBindMount on RedisResource
cache2.withDataBindMount("/tmp/redis-data");

// withHostPort on RedisResource
cache.withHostPort({ port: 6379 });

// withPassword on RedisResource
const newPassword = await builder.addParameter("new-redis-password", { secret: true });
cache2.withPassword(newPassword);

// withRedisCommander — with configureContainer callback exercising withHostPort
cache.withRedisCommander({
    configureContainer: async (commander) => {
        commander.withHostPort({ port: 8081 });
    },
    containerName: "my-commander"
});

// withRedisInsight — with configureContainer callback exercising withHostPort, withDataVolume, withDataBindMount
cache.withRedisInsight({
    configureContainer: async (insight) => {
        insight.withHostPort({ port: 5540 });
        insight.withRedisInsightDataVolume({ name: "insight-data" });
        insight.withRedisInsightDataBindMount("/tmp/insight-data");
    },
    containerName: "my-insight"
});

// ---- Property access on RedisResource (ExposeProperties = true) ----
const redis = await cache;
const _endpoint = await redis.primaryEndpoint.get();
const _host = await redis.host.get();
const _port = await redis.port.get();
const _tlsEnabled: boolean = await redis.tlsEnabled.get();
const _uri = await redis.uriExpression.get();

await builder.build().run();
