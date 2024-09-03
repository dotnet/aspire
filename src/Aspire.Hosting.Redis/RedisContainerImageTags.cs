// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Redis;

internal static class RedisContainerImageTags
{
    public const string Registry = "docker.io";
    public const string Image = "library/redis";
    public const string Tag = "7.4";
    public const string RedisCommanderRegistry = "docker.io";
    public const string RedisCommanderImage = "rediscommander/redis-commander";
    public const string RedisCommanderTag = "latest";
    public const string RedisInsightRegistry = "docker.io";
    public const string RedisInsightImage = "redis/redisinsight";
    public const string RedisInsightTag = "2.54";
}
